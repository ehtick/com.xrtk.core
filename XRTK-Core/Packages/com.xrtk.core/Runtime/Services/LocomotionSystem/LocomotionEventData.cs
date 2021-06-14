﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine.EventSystems;
using XRTK.EventDatum;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.LocomotionSystem;

namespace XRTK.Services.LocomotionSystem
{
    /// <summary>
    /// Describes a locomotion event raised by the <see cref="IMixedRealityLocomotionSystem"/>.
    /// </summary>
    public class LocomotionEventData : GenericBaseEventData
    {
        /// <summary>
        /// The locomotion provider the event was raised for or raised by.
        /// </summary>
        public ILocomotionProvider LocomotionProvider { get; private set; }

        /// <summary>
        /// The pointer that raised the event.
        /// </summary>
        public IMixedRealityPointer Pointer { get; private set; }

        /// <summary>
        /// The teleport hot spot.
        /// </summary>
        public ITeleportHotSpot HotSpot { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventSystem">Typically will be <see cref="EventSystem.current"/></param>
        public LocomotionEventData(EventSystem eventSystem) : base(eventSystem) { }

        /// <summary>
        /// Used to initialize/reset the event and populate the data.
        /// </summary>
        /// <param name="locomotionProvider">The <see cref="ILocomotionProvider"/> the event data is addressed at or coming from.</param>
        /// <param name="pointer">Optional <see cref="IMixedRealityPointer"/> providing a teleport target.</param>
        /// <param name="hotSpot">Optional <see cref="ITeleportHotSpot"/> at the teleport target location.</param>
        public void Initialize(ILocomotionProvider locomotionProvider, IMixedRealityPointer pointer, ITeleportHotSpot hotSpot)
        {
            BaseInitialize(pointer.InputSourceParent);
            LocomotionProvider = locomotionProvider;
            Pointer = pointer;
            HotSpot = hotSpot;
        }

        /// <summary>
        /// Used to initialize/reset the event and populate the data.
        /// </summary>
        /// <param name="locomotionProvider">The <see cref="ILocomotionProvider"/> the event data is addressed at or coming from.</param>
        /// <param name="inputSource">The <see cref="IMixedRealityInputSource"/> the event originated from.</param>
        public void Initialize(ILocomotionProvider locomotionProvider, IMixedRealityInputSource inputSource)
        {
            BaseInitialize(inputSource);
            LocomotionProvider = locomotionProvider;
            Pointer = null;
            HotSpot = null;
        }
    }
}
