/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.DistanceReticles
{
    public abstract class InteractorReticle<TReticleData> : MonoBehaviour
        where TReticleData : IReticleData
    {
        protected abstract IDistanceInteractor DistanceInteractor { get; set; }

        private TReticleData _targetData;
        private bool _drawing;
        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(DistanceInteractor);
            Hide();
            this.EndStart(ref _started);
        }
        protected virtual void OnEnable()
        {
            if (_started)
            {
                DistanceInteractor.WhenStateChanged += HandleStateChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                DistanceInteractor.WhenStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(InteractorStateChangeArgs args)
        {
            if (args.NewState == InteractorState.Normal
                && args.PreviousState != InteractorState.Disabled)
            {
                InteractableUnset();
            }
            else if(args.NewState == InteractorState.Select)
            {
                InteractableUnset();
            }
            else if (args.NewState == InteractorState.Hover)
            {
                InteractableSet(DistanceInteractor.Candidate as MonoBehaviour);
            }
        }

        #region Drawing
        protected abstract void Draw(TReticleData data);
        protected abstract void Hide();
        protected abstract void Align(TReticleData data, ConicalFrustum frustum);
        #endregion

        private void InteractableSet(MonoBehaviour interactableComponent)
        {
            if (interactableComponent != null
                && interactableComponent.TryGetComponent(out TReticleData reticleData))
            {
                _targetData = reticleData;
                Draw(reticleData);
                Align(reticleData, DistanceInteractor.PointerFrustum);
                _drawing = true;
            }
        }

        private void InteractableUnset()
        {
            if (_drawing)
            {
                Hide();
                _targetData = default(TReticleData);
                _drawing = false;
            }
        }

        protected virtual void LateUpdate()
        {
            if (_drawing)
            {
                Align(_targetData, DistanceInteractor.PointerFrustum);
            }
        }
    }
}
