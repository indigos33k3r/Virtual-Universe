/*
 * Copyright (c) Contributors, http://virtual-planets.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 * For an explanation of the license of each contributor and the content it 
 * covers please see the Licenses directory.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Virtual Universe Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using Universe.Framework.Physics;
using Universe.Framework.SceneInfo;
using Universe.Framework.Utilities;
using OMV = OpenMetaverse;

namespace Universe.Physics.BulletSPlugin
{
    /*
     * Class to wrap all objects.
     * The rest of BulletSim doesn't need to keep checking for avatars or prims
     *        unless the difference is significant.
     * 
     *  Variables in the physicsl objects are in three forms:
     *      VariableName: used by the simulator and performs taint operations, etc
     *      RawVariableName: direct reference to the BulletSim storage for the variable value
     *      ForceVariableName: direct reference (store and fetch) to the value in the physics engine.
     *  The last one should only be referenced in taint-time.
     */

    /*
     * As of 20121221, the following are the call sequences (going down) for different script physical functions:
     * llApplyImpulse       llApplyRotImpulse           llSetTorque             llSetForce
     * SOP.ApplyImpulse     SOP.ApplyAngularImpulse     SOP.SetAngularImpulse   SOP.SetForce
     * SOG.ApplyImpulse     SOG.ApplyAngularImpulse     SOG.SetAngularImpulse
     * PA.AddForce          PA.AddAngularForce          PA.Torque = v           PA.Force = v
     * BS.ApplyCentralForce BS.ApplyTorque              
     */

    // Flags used to denote which properties updates when making UpdateProperties calls to linksets, etc.
    public enum UpdatedProperties : uint
    {
        Position = 1 << 0,
        Orientation = 1 << 1,
        Velocity = 1 << 2,
        Acceleration = 1 << 3,
        RotationalVelocity = 1 << 4,
        EntPropUpdates = Position | Orientation | Velocity | Acceleration | RotationalVelocity,
    }

    public abstract class BSPhysObject : PhysicsActor
    {
        protected BSPhysObject()
        {
        }

        protected BSPhysObject(BSScene parentScene, uint localID, string name, string typeName)
        {
            PhysicsScene = parentScene;
            LocalID = localID;
            PhysObjectName = name;
            Name = name; // PhysicsActor also has the name of the object. Someday consolidate.
            TypeName = typeName;

            // Oddity if object is destroyed and recreated very quickly it could still have the old body.
            if (!PhysBody.HasPhysicalBody)
                PhysBody = new BulletBody(localID);

            // The collection of things that push me around
            PhysicalActors = new BSActorCollection(PhysicsScene);

            // Initialize variables kept in base.
            GravityMultiplier = 1.0f;
            Gravity = new OMV.Vector3(0f, 0f, BSParam.Gravity);

            PrimAssetState = PrimAssetCondition.Unknown;

            // Default material type. Also sets Friction, Restitution and Density.
            SetMaterial((int)MaterialAttributes.Material.Wood);

            CollisionCollection = new CollisionEventUpdate();
            CollisionsLastTick = CollisionCollection;
            SubscribedEventsMs = 0;
            CollidingStep = 0;
            TrueCollidingStep = 0;
            CollisionAccumulation = 0;
            ColliderIsMoving = false;
            CollisionScore = 0;

            // All axis free.
            LockedLinearAxis = LockedAxisFree;
            LockedAngularAxis = LockedAxisFree;
        }

        // Tell the object to clean up.
        public virtual void Destroy()
        {
            PhysicalActors.Enable(false);
            PhysicsScene.TaintedObject(LocalID, "BSPhysObject.Destroy", delegate() { PhysicalActors.Dispose(); });
        }

        public BSScene PhysicsScene { get; protected set; }
        // public override uint LocalID { get; set; } // Use the LocalID definition in PhysicsActor
        public string PhysObjectName { get; protected set; }
        public string TypeName { get; protected set; }

        // Set to 'true' when the object is completely initialized.
        // This mostly prevents property update and collisions until the object is complete here.
        public bool IsInitialized { get; protected set; }

        // Set to 'true' if an object (mesh/linkset/sculpty) is not completely constructed.
        // This test is used to prevent some updates to the object when it only partially exists.
        // There are several reasons and object might be incomplete:
        //     Its underlying mesh/sculpty is an asset which must be fetched from the asset store
        //     It is a linkset who is being added to or removed from
        //     It is changing state (static to physical, for instance) which requires rebuilding
        // This is a computed value based on the underlying physical object construction
        abstract public bool IsIncomplete { get; }

        // Return the object mass without calculating it or having side effects
        public abstract float RawMass { get; }
        // Set the raw mass but also update physical mass properties (inertia, ...)
        // 'inWorld' true if the object has already been added to the dynamic world.
        public abstract void UpdatePhysicalMassProperties(float mass, bool inWorld);

        // The gravity being applied to the object. A function of default grav, GravityModifier and Buoyancy.
        public virtual OMV.Vector3 Gravity { get; set; }
        // The last value calculated for the prim's inertia
        public OMV.Vector3 Inertia { get; set; }

        // Reference to the physical body (btCollisionObject) of this object
        public BulletBody PhysBody = new BulletBody(0);
        // Reference to the physical shape (btCollisionShape) of this object
        public BSShape PhysShape = new BSShapeNull();

        // The physical representation of the prim might require an asset fetch.
        // The asset state is first 'Unknown' then 'Waiting' then either 'Failed' or 'Fetched'.
        public enum PrimAssetCondition
        {
            Unknown,
            Waiting,
            FailedAssetFetch,
            FailedMeshing,
            Fetched
        }

        public PrimAssetCondition PrimAssetState { get; set; }
        public virtual bool AssetFailed()
        {
            return ( (PrimAssetState == PrimAssetCondition.FailedAssetFetch)
                  || (PrimAssetState == PrimAssetCondition.FailedMeshing) );
        }

        // The objects base shape information. Null if not a prim type shape.
        public PrimitiveBaseShape BaseShape { get; protected set; }

        // Some types of objects have preferred physical representations.
        // Returns SHAPE_UNKNOWN if there is no preference.
        public virtual BSPhysicsShapeType PreferredPhysicalShape
        {
            get { return BSPhysicsShapeType.SHAPE_UNKNOWN; }
        }

        // When the physical properties are updated, an EntityProperty holds the update values.
        // Keep the current and last EntityProperties to enable computation of differences
        //      between the current update and the previous values.
        public EntityProperties CurrentEntityProperties { get; set; }
        public EntityProperties LastEntityProperties { get; set; }

        public virtual OMV.Vector3 Scale { get; set; }

        // It can be confusing for an actor to know if it should move or update an object
        //    depeneding on the setting of 'selected', 'physical, ...
        // This flag is the true test -- if true, the object is being acted on in the physical world
        public abstract bool IsPhysicallyActive { get; }

        // Detailed state of the object.
        public abstract bool IsSolid { get; }
        public abstract bool IsStatic { get; }
        public abstract bool IsSelected { get; }
        public abstract bool IsVolumeDetect { get; }

        // Materialness
        public MaterialAttributes.Material Material { get; private set; }

        public void SetMaterial(int material)
        {
            Material = (MaterialAttributes.Material)material;

            // Setting the material sets the material attributes also.
            MaterialAttributes matAttrib = BSMaterials.GetAttributes(Material, false);
            Friction = matAttrib.friction;
            Restitution = matAttrib.restitution;
            Density = matAttrib.density / BSParam.DensityScaleFactor;
            // DetailLog("{0},{1}.SetMaterial,Mat={2},frict={3},rest={4},den={5}", LocalID, TypeName, Material, Friction, Restitution, Density);
        }

    	  public override float Density
        {
            get { return base.Density; }
            set { base.Density = value; }
        }

        // Stop all physical motion.
        public abstract void ZeroMotion(bool inTaintTime);
        public abstract void ZeroAngularMotion(bool inTaintTime);

        // Update the physical location and motion of the object. Called with data from Bullet.
        public abstract void UpdateProperties(EntityProperties entprop);

        public abstract OMV.Vector3 RawPosition { get; set; }
        public abstract OMV.Vector3 ForcePosition { get; set; }

        public abstract OMV.Quaternion RawOrientation { get; set; }
        public abstract OMV.Quaternion ForceOrientation { get; set; }

        public OMV.Vector3 RawVelocity { get; set; }
        public abstract OMV.Vector3 ForceVelocity { get; set; }

        public OMV.Vector3 RawForce { get; set; }
        public OMV.Vector3 RawTorque { get; set; }

        public override void AddAngularForce(OMV.Vector3 force, bool pushforce)
        {
            AddAngularForce(force, pushforce, false);
        }

        public abstract void AddAngularForce(OMV.Vector3 force, bool pushforce, bool inTaintTime);

        public abstract OMV.Vector3 ForceRotationalVelocity { get; set; }

        public abstract float ForceBuoyancy { get; set; }

        public virtual bool ForceBodyShapeRebuild(bool inTaintTime)
        {
            return false;
        }

        // The current velocity forward
        public virtual float ForwardSpeed
        {
            get
            {
                OMV.Vector3 characterOrientedVelocity = RawVelocity *
                                                        OMV.Quaternion.Inverse(OMV.Quaternion.Normalize(RawOrientation));
                return characterOrientedVelocity.X;
            }
        }

        // The forward speed we are trying to achieve (TargetVelocity)
        public virtual float TargetVelocitySpeed
        {
            get
            {
                OMV.Vector3 characterOrientedVelocity = TargetVelocity *
                                                        OMV.Quaternion.Inverse(OMV.Quaternion.Normalize(RawOrientation));
                return characterOrientedVelocity.X;
            }
        }

        // The user can optionally set the center of mass. The user's setting will override any
        //    computed center-of-mass (like in linksets).
        // Note this is a displacement from the root's coordinates. Zero means use the root prim as center-of-mass.
        public OMV.Vector3? UserSetCenterOfMassDisplacement { get; set; }

        public OMV.Vector3 LockedLinearAxis;    // zero means locked. one means free.
        public OMV.Vector3 LockedAngularAxis;   // zero means locked. one means free.
        public const float FreeAxis = 1f;
        public const float LockedAxis = 0f;
        public readonly OMV.Vector3 LockedAxisFree = new OMV.Vector3(FreeAxis, FreeAxis, FreeAxis); // All axis are free

        // If an axis is locked (flagged above) then the limits of that axis are specified here.
        // Linear axis limits are relative to the object's starting coordinates.
        // Angular limits are limited to -PI to +PI
        public OMV.Vector3 LockedLinearAxisLow;
        public OMV.Vector3 LockedLinearAxisHigh;
        public OMV.Vector3 LockedAngularAxisLow;
        public OMV.Vector3 LockedAngularAxisHigh;

        // Enable physical actions. Bullet will keep sleeping non-moving physical objects so
        //     they need waking up when parameters are changed.
        // Called in taint-time!!
        public void ActivateIfPhysical(bool forceIt)
        {
            //if (IsPhysical && PhysBody.HasPhysicalBody)
            if (PhysBody.HasPhysicalBody)
            {
                if (IsPhysical)
                    PhysicsScene.PE.Activate(PhysBody, forceIt);
                else
                    PhysicsScene.PE.ClearCollisionProxyCache(PhysicsScene.World, PhysBody);
            }
        }

        // 'actors' act on the physical object to change or constrain its motion. These can range from
        //       hovering to complex vehicle motion.
        // May be called at non-taint time as this just adds the actor to the action list and the real
        //    work is done during the simulation step.
        // Note that, if the actor is already in the list and we are disabling same, the actor is just left
        //    in the list disabled.
        public delegate BSActor CreateActor();

        public void EnableActor(bool enableActor, string actorName, CreateActor creator)
        {
            lock (PhysicalActors)
            {
                BSActor theActor;
                if (PhysicalActors.TryGetActor(actorName, out theActor))
                {
                    // The actor already exists so just turn it on or off
                    DetailLog("{0},BSPhysObject.EnableActor,enableExistingActor,name={1},enable={2}", LocalID, actorName, enableActor);
                    theActor.Enabled = enableActor;
                }
                else
                {
                    // The actor does not exist. If it should, create it.
                    if (enableActor)
                    {
                        DetailLog("{0},BSPhysObject.EnableActor,creatingActor,name={1}", LocalID, actorName);
                        theActor = creator();
                        PhysicalActors.Add(actorName, theActor);
                        theActor.Enabled = true;
                    }
                    else
                    {
                        DetailLog("{0},BSPhysobject.EnableActor,notCreatingActorSinceNotEnabled,name={1}", LocalID, actorName);
                    }
                }
            }
        }

        #region Collisions

        // Requested number of milliseconds between collision events. Zero means disabled.
        protected int SubscribedEventsMs { get; set; }
        // Given subscription, the time that a collision may be passed up
        protected int NextCollisionOkTime { get; set; }
        // The simulation step that last had a collision
        protected long CollidingStep { get; set; }
        // The simulation step that last had a collision
        protected long TrueCollidingStep { get; set; }
        // The collision flags we think are set in Bullet
        protected CollisionFlags CurrentCollisionFlags { get; set; }
        // On a collision, check the collider and remember if the last collider was moving
        //    Used to modify the standing of avatars (avatars on stationary things stand still)
        public bool ColliderIsMoving;
    	  // 'true' if the last collider was a volume detect object
        public bool ColliderIsVolumeDetect;
        // Used by BSCharacter to manage standing (and not slipping)
        public bool IsStationary;

        // Count of collisions for this object
        protected long CollisionAccumulation { get; set; }

        public override bool IsColliding
        {
            get { return (CollidingStep == PhysicsScene.SimulationStep); }
            set
            {
                if (value)
                {
                    TrueCollidingStep = PhysicsScene.SimulationStep;
                    CollidingStep = PhysicsScene.SimulationStep;
                }
                else
                {
                    TrueCollidingStep = 0;
                    CollidingStep = 0;
                }
            }
        }

        public override bool IsTruelyColliding
        {
            get { return (TrueCollidingStep == PhysicsScene.SimulationStep); }
            set
            {
                if (value)
                {
                    TrueCollidingStep = PhysicsScene.SimulationStep;
                    CollidingStep = PhysicsScene.SimulationStep;
                }
                else
                {
                    TrueCollidingStep = 0;
                    CollidingStep = 0;
                }
            }
        }

        // The collisions that have been collected this tick
        protected CollisionEventUpdate CollisionCollection;
        // Remember collisions from last tick for fancy collision based actions
        //     (like a BSCharacter walking up stairs).
        public CollisionEventUpdate CollisionsLastTick;

        // The simulation step is telling this object about a collision.
        // Return 'true' if a collision was processed and should be sent up.
        // Return 'false' if this object is not enabled/subscribed/appropriate for or has already seen this collision.
        // Called at taint time from within the Step() function
        public virtual bool Collide(uint collidingWith, BSPhysObject collidee,
            OMV.Vector3 contactPoint, OMV.Vector3 contactNormal, float pentrationDepth)
        {
            bool ret = false;
            bool p2col = true;

            // We only need to test p2 for 'jump crouch purposes'
            if (TypeName == "BSCharacter" && collidee is BSPrim)
            {
                // Testing if the collision is at the feet of the avatar
                if ((Position.Z - contactPoint.Z) < (Size.Z * 0.5f))
                    p2col = false;
            }

            // The following lines make IsColliding(), CollidingGround() and CollidingObj work
            if (p2col)
                CollidingStep = PhysicsScene.SimulationStep;
            TrueCollidingStep = PhysicsScene.SimulationStep;

            CollisionAccumulation++;

            // For movement tests, remember if we are colliding with an object that is moving.
            ColliderIsMoving = collidee != null ? (collidee.RawVelocity != OMV.Vector3.Zero) : false;
            ColliderIsVolumeDetect = collidee != null ? (collidee.IsVolumeDetect) : false;

            // If someone has subscribed for collision events log the collision so it will be reported up
            if (SubscribedEvents())
            {
                CollisionCollection.AddCollider(collidingWith,
                    new ContactPoint(contactPoint, contactNormal, pentrationDepth, (ActorTypes)PhysicsActorType));
                DetailLog(
                    "{0},{1}.Collison.AddCollider,call,with={2},point={3},normal={4},depth={5},colliderMoving={6}",
                    LocalID, TypeName, collidingWith, contactPoint, contactNormal, pentrationDepth, ColliderIsMoving);

                ret = true;
            }
            return ret;
        }

        public override void AddCollisionEvent(uint localID, ContactPoint contact)
        {
            CollisionCollection.AddCollider(localID, contact);
        }

        // Send the collected collisions into the simulator.
        // Called at taint time from within the Step() function thus no locking problems
        //      with CollisionCollection and ObjectsWithNoMoreCollisions.
        // Return 'true' if there were some actual collisions passed up
        public override bool SendCollisions()
        {
            bool ret = true;

            // If the 'no collision' call, force it to happen right now so quick collision_end
            bool force = (CollisionCollection.Count == 0 && CollisionsLastTick.Count != 0);

            // throttle the collisions to the number of milliseconds specified in the subscription
            if (force || (PhysicsScene.SimulationNowTime >= NextCollisionOkTime))
            {
                NextCollisionOkTime = PhysicsScene.SimulationNowTime + SubscribedEventsMs;

                // We are called if we previously had collisions. If there are no collisions
                //   this time, send up one last empty event so Universe can sense collision end.
                if (CollisionCollection.Count == 0)
                {
                    // If I have no collisions this time, remove me from the list of objects with collisions.
                    ret = false;
                }

                DetailLog("{0},{1}.SendCollisionUpdate,call,numCollisions={2}", LocalID, TypeName,
                    CollisionCollection.Count);
                base.SendCollisionUpdate(CollisionCollection);

                // Remember the collisions from this tick for some collision specific processing.
                CollisionsLastTick = CollisionCollection;

                // The CollisionCollection instance is passed around in the simulator.
                // Make sure we don't have a handle to that one and that a new one is used for next time.
                //    This fixes an interesting 'gotcha'. If we call CollisionCollection.Clear() here, 
                //    a race condition is created for the other users of this instance.
                CollisionCollection = new CollisionEventUpdate();
            }
            return ret;
        }

        // Subscribe for collision events.
        // Parameter is the millisecond rate the caller wishes collision events to occur.
        public override void SubscribeEvents(int ms)
        {
            // DetailLog("{0},{1}.SubscribeEvents,subscribing,ms={2}", LocalID, TypeName, ms);
            SubscribedEventsMs = ms;
            if (ms > 0)
            {
                // make sure first collision happens
                NextCollisionOkTime = Util.EnvironmentTickCountSubtract(SubscribedEventsMs);

                PhysicsScene.TaintedObject(LocalID, TypeName + ".SubscribeEvents", delegate()
                {
                    if (PhysBody.HasPhysicalBody)
                        CurrentCollisionFlags = PhysicsScene.PE.AddToCollisionFlags(PhysBody,
                            CollisionFlags.BS_SUBSCRIBE_COLLISION_EVENTS);
                });
            }
            else
            {
                // Subscribing for zero or less is the same as unsubscribing
                UnSubscribeEvents();
            }
        }

        public override void UnSubscribeEvents()
        {
            // DetailLog("{0},{1}.UnSubscribeEvents,unsubscribing", LocalID, TypeName);
            SubscribedEventsMs = 0;
            PhysicsScene.TaintedObject(LocalID, TypeName + ".UnSubscribeEvents", delegate()
            {
                // Make sure there is a body there because sometimes destruction happens in an un-ideal order.
                if (PhysBody.HasPhysicalBody)
                    CurrentCollisionFlags = PhysicsScene.PE.RemoveFromCollisionFlags(PhysBody,
                        CollisionFlags.BS_SUBSCRIBE_COLLISION_EVENTS);
            });
        }

        // Return 'true' if the simulator wants collision events
        public override bool SubscribedEvents()
        {
            return (SubscribedEventsMs > 0);
        }

        // Because 'CollisionScore' is called many times while sorting, it should not be recomputed
        //    each time called. So this is built to be light weight for each collision and to do
        //    all the processing when the user asks for the info.
        public void ComputeCollisionScore()
        {
            // Scale the collision count by the time since the last collision.
            // The "+1" prevents dividing by zero.
            long timeAgo = PhysicsScene.SimulationStep - CollidingStep + 1;
            CollisionScore = CollisionAccumulation / timeAgo;
        }

        public override float CollisionScore { get; set; }
        public bool MoveToTargetActive { get; set; }
        public OMV.Vector3 MoveToTargetTarget { get; set; }
        public float MoveToTargetTau { get; set; }

        #endregion // Collisions

        #region Per Simulation Step actions

        public BSActorCollection PhysicalActors;

        // When an update to the physical properties happens, this event is fired to let
        //    different actors to modify the update before it is passed around
        public delegate void PreUpdatePropertyAction(ref EntityProperties entprop);

        public event PreUpdatePropertyAction OnPreUpdateProperty;

        protected void TriggerPreUpdatePropertyAction(ref EntityProperties entprop)
        {
            PreUpdatePropertyAction actions = OnPreUpdateProperty;
            if (actions != null)
                actions(ref entprop);
        }

        #endregion // Per Simulation Step actions

        // High performance detailed logging routine used by the physical objects.
        protected void DetailLog(string msg, params Object[] args)
        {
            //if (PhysicsScene.PhysicsLogging.Enabled)
            PhysicsScene.DetailLog(msg, args);
        }
    }
}
