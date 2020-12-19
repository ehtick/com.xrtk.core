// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using XRTK.Definitions.TeleportSystem;
using XRTK.Definitions.Utilities;
using XRTK.EventDatum.Teleport;
using XRTK.Extensions;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.TeleportSystem;
using XRTK.Interfaces.TeleportSystem.Handlers;
using XRTK.Utilities;
using Object = UnityEngine.Object;

namespace XRTK.Services.Teleportation
{
    /// <summary>
    /// The Mixed Reality Toolkit's specific implementation of the <see cref="IMixedRealityTeleportSystem"/>
    /// </summary>
    [System.Runtime.InteropServices.Guid("2C18630A-9837-46FA-ADDF-6C8AF34D4143")]
    public class MixedRealityTeleportSystem : BaseEventSystem, IMixedRealityTeleportSystem
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile">The active <see cref="MixedRealityTeleportSystemProfile"/>.</param>
        public MixedRealityTeleportSystem(MixedRealityTeleportSystemProfile profile)
            : base(profile)
        {
            teleportMode = profile.TeleportMode;
            if (teleportMode == TeleportMode.Provider)
            {
                if (profile.TeleportProvider?.Type == null)
                {
                    throw new Exception($"The {nameof(MixedRealityTeleportSystemProfile)} is configured for " +
                        $"{TeleportMode.Provider} but is missing the required {teleportProvider}!");
                }

                teleportProvider = profile.TeleportProvider;
            }
        }

        private readonly SystemType teleportProvider;
        private readonly TeleportMode teleportMode;

        private TeleportEventData teleportEventData;
        private bool isTeleporting = false;

        #region IMixedRealityService Implementation

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            if (Application.isPlaying)
            {
                teleportEventData = new TeleportEventData(EventSystem.current);
            }

            switch (teleportMode)
            {
                case TeleportMode.Default:
                    var component = CameraCache.Main.GetComponent<IMixedRealityTeleportProvider>() as Component;
                    if (!component.IsNull())
                    {
                        if (Application.isPlaying)
                        {
                            Object.Destroy(component);
                        }
                        else
                        {
                            Object.DestroyImmediate(component);
                        }
                    }
                    break;
                case TeleportMode.Provider:
                    CameraCache.Main.gameObject.EnsureComponent(teleportProvider.Type);
                    break;
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            base.Disable();

            if (!Application.isPlaying)
            {
                var component = CameraCache.Main.GetComponent<IMixedRealityTeleportProvider>() as Component;
                if (!component.IsNull())
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        #endregion IMixedRealityService Implementation

        #region IEventSystemManager Implementation

        /// <inheritdoc />
        public override void HandleEvent<T>(BaseEventData eventData, ExecuteEvents.EventFunction<T> eventHandler)
        {
            Debug.Assert(eventData != null);
            var teleportData = ExecuteEvents.ValidateEventData<TeleportEventData>(eventData);
            Debug.Assert(teleportData != null);
            Debug.Assert(!teleportData.used);

            // Process all the event listeners
            base.HandleEvent(teleportData, eventHandler);
        }

        /// <summary>
        /// Unregister a <see cref="GameObject"/> from listening to Teleport events.
        /// </summary>
        /// <param name="listener"></param>
        public override void Register(GameObject listener)
        {
            base.Register(listener);
        }

        /// <summary>
        /// Unregister a <see cref="GameObject"/> from listening to Teleport events.
        /// </summary>
        /// <param name="listener"></param>
        public override void Unregister(GameObject listener)
        {
            base.Unregister(listener);
        }

        #endregion IEventSystemManager Implementation

        #region IMixedRealityTeleportSystem Implementation

        private static readonly ExecuteEvents.EventFunction<IMixedRealityTeleportHandler> OnTeleportRequestHandler =
            delegate (IMixedRealityTeleportHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<TeleportEventData>(eventData);
                handler.OnTeleportRequest(casted);
            };

        /// <inheritdoc />
        public void RaiseTeleportRequest(IMixedRealityPointer pointer, IMixedRealityTeleportHotSpot hotSpot)
        {
            // initialize event
            teleportEventData.Initialize(pointer, hotSpot);

            // Pass handler
            HandleEvent(teleportEventData, OnTeleportRequestHandler);
        }

        private static readonly ExecuteEvents.EventFunction<IMixedRealityTeleportHandler> OnTeleportStartedHandler =
            delegate (IMixedRealityTeleportHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<TeleportEventData>(eventData);
                handler.OnTeleportStarted(casted);
            };

        /// <inheritdoc />
        public void RaiseTeleportStarted(IMixedRealityPointer pointer, IMixedRealityTeleportHotSpot hotSpot)
        {
            if (isTeleporting)
            {
                Debug.LogError("Teleportation already in progress");
                return;
            }

            isTeleporting = true;

            // initialize event
            teleportEventData.Initialize(pointer, hotSpot);

            // Pass handler
            HandleEvent(teleportEventData, OnTeleportStartedHandler);

            // In default teleportation mode we do not expect any provider
            // to handle teleportation, instead we simply perform an instant teleport.
            if (teleportMode == TeleportMode.Default)
            {
                PerformDefaultTeleport(teleportEventData);
            }
        }

        private static readonly ExecuteEvents.EventFunction<IMixedRealityTeleportHandler> OnTeleportCompletedHandler =
            delegate (IMixedRealityTeleportHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<TeleportEventData>(eventData);
                handler.OnTeleportCompleted(casted);
            };

        /// <inheritdoc />
        public void RaiseTeleportComplete(IMixedRealityPointer pointer, IMixedRealityTeleportHotSpot hotSpot)
        {
            if (!isTeleporting)
            {
                Debug.LogError("No Active Teleportation in progress.");
                return;
            }

            // initialize event
            teleportEventData.Initialize(pointer, hotSpot);

            // Pass handler
            HandleEvent(teleportEventData, OnTeleportCompletedHandler);

            isTeleporting = false;
        }

        private static readonly ExecuteEvents.EventFunction<IMixedRealityTeleportHandler> OnTeleportCanceledHandler =
            delegate (IMixedRealityTeleportHandler handler, BaseEventData eventData)
            {
                var casted = ExecuteEvents.ValidateEventData<TeleportEventData>(eventData);
                handler.OnTeleportCanceled(casted);
            };

        /// <inheritdoc />
        public void RaiseTeleportCanceled(IMixedRealityPointer pointer, IMixedRealityTeleportHotSpot hotSpot)
        {
            // initialize event
            teleportEventData.Initialize(pointer, hotSpot);

            // Pass handler
            HandleEvent(teleportEventData, OnTeleportCanceledHandler);

            isTeleporting = false;
        }

        #endregion IMixedRealityTeleportSystem Implementation

        private void PerformDefaultTeleport(TeleportEventData eventData)
        {
            var cameraTransform = MixedRealityToolkit.CameraSystem != null ?
                MixedRealityToolkit.CameraSystem.MainCameraRig.CameraTransform :
                CameraCache.Main.transform;
            var teleportTransform = cameraTransform.parent;
            Debug.Assert(teleportTransform != null,
                $"{nameof(TeleportMode.Default)} requires that the camera be parented under another object!");

            var targetRotation = Vector3.zero;
            var targetPosition = eventData.Pointer.Result.EndPoint;
            targetRotation.y = eventData.Pointer.PointerOrientation;

            if (eventData.HotSpot != null)
            {
                targetPosition = eventData.HotSpot.Position;
                if (eventData.HotSpot.OverrideTargetOrientation)
                {
                    targetRotation.y = eventData.HotSpot.TargetOrientation;
                }
            }

            var height = targetPosition.y;
            targetPosition -= cameraTransform.position - teleportTransform.position;
            targetPosition.y = height;
            teleportTransform.position = targetPosition;
            teleportTransform.RotateAround(cameraTransform.position, Vector3.up, targetRotation.y - cameraTransform.eulerAngles.y);

            RaiseTeleportComplete(eventData.Pointer, eventData.HotSpot);
        }
    }
}
