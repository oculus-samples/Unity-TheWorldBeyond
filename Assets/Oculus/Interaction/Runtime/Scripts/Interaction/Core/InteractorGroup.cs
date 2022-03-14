/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// InteractorGroup coordinates between a set of Interactors to
    /// determine which Interactor(s) should be enabled at a time.
    ///
    /// By default, Interactors are prioritized in list order (first = highest priority).
    /// Interactors can also be prioritized with an optional ICandidateComparer
    /// </summary>
    public class InteractorGroup : MonoBehaviour, IInteractor
    {
        [FormerlySerializedAs("_interactorDrivers")]
        [SerializeField, Interface(typeof(IInteractor))]
        private List<MonoBehaviour> _interactors;

        protected List<IInteractor> Interactors;

        public bool IsRootDriver { get; set; } = true;

        private IInteractor _candidateInteractor = null;
        private IInteractor _selectingInteractor = null;

        [SerializeField, Interface(typeof(ICandidateComparer)), Optional]
        private MonoBehaviour _interactorComparer;

        public int MaxIterationsPerFrame = 3;

        protected ICandidateComparer CandidateComparer = null;

        protected virtual void Awake()
        {
            Interactors = _interactors.ConvertAll(mono => mono as IInteractor);
            CandidateComparer = _interactorComparer as ICandidateComparer;
        }

        protected virtual void Start()
        {
            foreach (IInteractor interactor in Interactors)
            {
                Assert.IsNotNull(interactor);
            }

            foreach (IInteractor interactor in Interactors)
            {
                interactor.IsRootDriver = false;
            }

            if (_interactorComparer != null)
            {
                Assert.IsNotNull(CandidateComparer);
            }
        }

        public void UpdateInteractor()
        {
            foreach (IInteractor interactor in Interactors)
            {
                interactor.UpdateInteractor();
            }
            WhenInteractorUpdated();
        }

        public void UpdateCandidate()
        {
            _candidateInteractor = null;

            foreach (IInteractor interactor in Interactors)
            {
                interactor.UpdateCandidate();

                if (interactor.HasCandidate)
                {
                    if (_candidateInteractor == null)
                    {
                        _candidateInteractor = interactor;
                    }
                    else if (Compare(_candidateInteractor, interactor) > 0)
                    {
                        _candidateInteractor = interactor;
                    }
                }
            }

            if (_candidateInteractor == null)
            {
                _candidateInteractor = Interactors[Interactors.Count - 1];
            }
        }

        public void Enable()
        {
            if (_candidateInteractor == null)
            {
                return;
            }
            _candidateInteractor.Enable();
            State = InteractorState.Normal;
        }

        public void Disable()
        {
            foreach (IInteractor interactor in Interactors)
            {
                interactor.Disable();
            }

            State = InteractorState.Disabled;
        }

        public void Hover()
        {
            if (_candidateInteractor == null)
            {
                return;
            }

            _candidateInteractor.Hover();
            State = InteractorState.Hover;
        }

        public void Select()
        {
            if (_selectingInteractor != null)
            {
                _selectingInteractor.Select();
                return;
            }

            if (!ShouldSelect)
            {
                return;
            }

            _selectingInteractor = _candidateInteractor;
            _selectingInteractor.Select();
            State = InteractorState.Select;
        }

        public void Unselect()
        {
            if (!ShouldUnselect)
            {
                return;
            }

            _selectingInteractor.Unselect();
            _selectingInteractor = null;
            _candidateInteractor = null;

            State = InteractorState.Hover;
        }

        public bool ShouldSelect => _candidateInteractor != null && _candidateInteractor.ShouldSelect;

        public bool ShouldUnselect =>
            _selectingInteractor != null && _selectingInteractor.ShouldUnselect;

        private void DisableAllInteractorsExcept(IInteractor enabledInteractor)
        {
            foreach (IInteractor interactor in Interactors)
            {
                if (interactor == enabledInteractor) continue;
                interactor.Disable();
            }
        }

        public int Identifier => _candidateInteractor != null
            ? _candidateInteractor.Identifier
            : Interactors[Interactors.Count - 1].Identifier;

        public bool HasCandidate => _candidateInteractor != null && _candidateInteractor.HasCandidate;

        public object Candidate => HasCandidate ? _candidateInteractor.Candidate : null;

        public bool HasInteractable =>
            _candidateInteractor != null && _candidateInteractor.HasInteractable;

        public bool HasSelectedInteractable => _selectingInteractor != null
            ? _selectingInteractor.HasSelectedInteractable
            : false;

        private InteractorState _state = InteractorState.Normal;

        public InteractorState State
        {
            get
            {
                return _state;
            }
            private set
            {
                if (_state == value)
                {
                    return;
                }
                InteractorState previousState = _state;
                _state = value;

                WhenStateChanged(new InteractorStateChangeArgs
                {
                    PreviousState = previousState,
                    NewState = _state
                });
            }
        }

        public event Action<InteractorStateChangeArgs> WhenStateChanged = delegate { };
        public event Action WhenInteractorUpdated = delegate { };

        public virtual void AddInteractor(IInteractor interactor)
        {
            Interactors.Add(interactor);
            _interactors.Add(interactor as MonoBehaviour);
            interactor.IsRootDriver = false;
        }

        public virtual void RemoveInteractor(IInteractor interactor)
        {
            Interactors.Remove(interactor);
            _interactors.Remove(interactor as MonoBehaviour);
            interactor.IsRootDriver = true;
        }

        private int Compare(IInteractor a, IInteractor b)
        {
            if (!a.HasCandidate && !b.HasCandidate)
            {
                return -1;
            }

            if (a.HasCandidate && b.HasCandidate)
            {
                if (CandidateComparer == null)
                {
                    return -1;
                }

                int result = CandidateComparer.Compare(a.Candidate, b.Candidate);
                return result > 0 ? 1 : -1;
            }

            return a.HasCandidate ? -1 : 1;
        }

        protected virtual void Update()
        {
            if (!IsRootDriver)
            {
                return;
            }

            UpdateInteractor();
            for (int i = 0; i < MaxIterationsPerFrame; i ++)
            {
                if (ShouldSelect || State == InteractorState.Select)
                {
                    Select();
                    if (!ShouldUnselect)
                    {
                        return;
                    }
                    Unselect();
                }

                UpdateCandidate();
                DisableAllInteractorsExcept(_candidateInteractor);
                Enable();

                Hover();

                if (!HasInteractable)
                {
                    return;
                }

                if (!ShouldSelect)
                {
                    return;
                }
            }
        }

        #region Inject

        public void InjectAllInteractorGroup(List<IInteractor> interactors)
        {
            InjectInteractors(interactors);
        }

        public void InjectInteractors(List<IInteractor> interactors)
        {
            Interactors = interactors;
            _interactors = interactors.ConvertAll(interactor => interactor as MonoBehaviour);
        }

        public void InjectOptionalInteractorComparer(ICandidateComparer comparer)
        {
            CandidateComparer = comparer;
            _interactorComparer = comparer as MonoBehaviour;
        }

        #endregion
    }
}
