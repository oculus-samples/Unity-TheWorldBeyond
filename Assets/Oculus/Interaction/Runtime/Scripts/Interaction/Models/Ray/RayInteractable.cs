/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using UnityEngine.Assertions;
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction
{
    public class RayInteractable : PointerInteractable<RayInteractor, RayInteractable>
    {
        [SerializeField]
        private Collider _collider;
        public Collider Collider { get => _collider; }

        [SerializeField, Optional, Interface(typeof(IPointableSurface))]
        private MonoBehaviour _surface = null;

        private IPointableSurface Surface;

        protected override void Awake()
        {
            base.Awake();
            Surface = _surface as IPointableSurface;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, base.Start);
            Assert.IsNotNull(_collider);
            this.EndStart(ref _started);
        }

        public bool Raycast(Ray ray, out SurfaceHit hit, in float maxDistance, in bool useSurface)
        {
            hit = new SurfaceHit();
            if (Collider.Raycast(ray, out RaycastHit raycastHit, maxDistance))
            {
                hit.Point = raycastHit.point;
                hit.Normal = raycastHit.normal;
                hit.Distance = raycastHit.distance;
                return true;
            }
            else if (useSurface && Surface != null)
            {
                return Surface.Raycast(ray, out hit, maxDistance);
            }
            return false;
        }

        #region Inject

        public void InjectAllRayInteractable(Collider collider)
        {
            InjectCollider(collider);
        }

        public void InjectCollider(Collider collider)
        {
            _collider = collider;
        }

        public void InjectOptionalSurface(IPointableSurface surface)
        {
            Surface = surface;
            _surface = surface as MonoBehaviour;
        }

        #endregion
    }
}
