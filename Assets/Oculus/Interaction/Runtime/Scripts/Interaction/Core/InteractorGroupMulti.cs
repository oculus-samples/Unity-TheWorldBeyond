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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// InteractorGroupMulti coordinates between a set of Interactors to
    /// determine which Interactor(s) should be enabled at a time. This group allows for
    /// multiple interactors to hover prior to one being selected.
    ///
    /// By default, Interactors are prioritized in list order (first = highest priority).
    /// Interactors can otherwise be prioritized with an optional ICandidateComparer
    /// </summary>
    public class InteractorGroupMulti : MonoBehaviour, IInteractor, IUpdateDriver
    {
        [SerializeField, Interface(typeof(IInteractor))]
        private List<MonoBehaviour> _interactors;

        protected List<IInteractor> Interactors;

        public bool IsRootDriver { get; set; } = true;

        private IInteractor _candidateInteractor = null;
        private IInteractor _selectCandidateInteractor = null;
        private IInteractor _selectInteractor = null;

        [SerializeField, Interface(typeof(ICandidateComparer)), Optional]
        private MonoBehaviour _interactorComparer;

        public int MaxIterationsPerFrame = 3;
        protected ICandidateComparer CandidateComparer = null;

        public event Action<InteractorStateChangeArgs> WhenStateChanged = delegate { };
        public event Action WhenPreprocessed = delegate { };
        public event Action WhenProcessed = delegate { };
        public event Action WhenPostprocessed = delegate { };

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

        public void Preprocess()
        {
            foreach (IInteractor interactor in Interactors)
            {
                interactor.Preprocess();
            }

            WhenPreprocessed();
        }

        public void Process()
        {
            foreach (IInteractor interactor in Interactors)
            {
                interactor.Process();
            }

            if (State == InteractorState.Hover)
            {
                foreach (IInteractor interactor in Interactors)
                {
                    if (interactor.ShouldHover)
                    {
                        interactor.Hover();
                        interactor.Process();
                    }

                    if (interactor.ShouldUnhover)
                    {
                        interactor.Unhover();
                        interactor.Process();
                    }
                }
            }

            ProcessSelectCandidateInteractor();

            WhenProcessed();
        }

        public void Postprocess()
        {
            foreach (IInteractor interactor in Interactors)
            {
                interactor.Postprocess();
            }

            WhenPostprocessed();
        }

        public void ProcessCandidate()
        {
            _candidateInteractor = null;

            foreach (IInteractor interactor in Interactors)
            {
                interactor.ProcessCandidate();

                if (interactor.HasCandidate)
                {
                    if (_candidateInteractor == null ||
                        Compare(_candidateInteractor, interactor) > 0)
                    {
                        _candidateInteractor = interactor;
                    }
                }
            }

            if (_candidateInteractor == null && Interactors.Count > 0)
            {
                _candidateInteractor = Interactors[Interactors.Count - 1];
            }
        }

        public void Enable()
        {
            foreach (IInteractor interactor in Interactors)
            {
                interactor.Enable();
            }
        }

        public void Disable()
        {
            for (int i = 0; i < Interactors.Count; i++)
            {
                Interactors[i].Disable();
            }

            if (State == InteractorState.Select)
            {
                State = InteractorState.Hover;
            }

            if (State == InteractorState.Hover)
            {
                State = InteractorState.Normal;
            }

            State = InteractorState.Disabled;
        }


        public void Hover()
        {
            State = InteractorState.Hover;
        }

        public void Unhover()
        {
            if (State != InteractorState.Hover)
            {
                return;
            }
            State = InteractorState.Normal;
        }

        private void ProcessSelectCandidateInteractor()
        {
            _selectCandidateInteractor = null;

            if (State != InteractorState.Hover)
            {
                return;
            }

            foreach (IInteractor interactor in Interactors)
            {
                if (interactor.State == InteractorState.Hover && interactor.ShouldSelect)
                {
                    if (_selectCandidateInteractor == null ||
                        Compare(_selectCandidateInteractor, interactor) > 0)
                    {
                        _selectCandidateInteractor = interactor;
                    }
                }
            }
        }

        public void Select()
        {
            _selectInteractor = _selectCandidateInteractor;
            _selectInteractor.Select();
            DisableAllInteractorsExcept(_selectInteractor);
            State = InteractorState.Select;
        }

        public void Unselect()
        {
            if (State != InteractorState.Select)
            {
                return;
            }

            if (_selectInteractor != null)
            {
                _selectInteractor.Unselect();
                _selectInteractor = null;
            }

            State = InteractorState.Hover;
            Enable();
        }

        public bool ShouldHover
        {
            get
            {
                if (State != InteractorState.Normal)
                {
                    return false;
                }

                foreach(IInteractor interactor in Interactors)
                {
                    if (interactor.ShouldHover)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool ShouldUnhover
        {
            get
            {
                if (State != InteractorState.Hover)
                {
                    return false;
                }

                foreach (IInteractor interactor in Interactors)
                {
                    if (interactor.State == InteractorState.Hover && !interactor.ShouldUnhover)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool ShouldSelect => _selectCandidateInteractor != null;

        public bool ShouldUnselect => _selectInteractor == null || _selectInteractor.ShouldUnselect;

        private void DisableAllInteractorsExcept(IInteractor enabledInteractor)
        {
            foreach (IInteractor interactor in Interactors)
            {
                if (interactor == enabledInteractor) continue;
                interactor.Disable();
            }
        }

        public int Identifier
        {
            get
            {
                if (_selectInteractor != null)
                {
                    return _selectInteractor.Identifier;
                }

                foreach (IInteractor interactor in Interactors)
                {
                    if (interactor.State == InteractorState.Hover || interactor.ShouldHover)
                    {
                        return interactor.Identifier;
                    }
                }

                return Interactors[Interactors.Count - 1].Identifier;
            }
        }

        public bool HasCandidate => _candidateInteractor != null && _candidateInteractor.HasCandidate;

        public object CandidateProperties => HasCandidate ? _candidateInteractor.CandidateProperties : null;

        public bool HasInteractable
        {
            get
            {
                if(_selectInteractor != null)
                {
                    return _selectInteractor.HasInteractable;
                }

                foreach (IInteractor interactor in Interactors)
                {
                    if (interactor.State == InteractorState.Hover)
                    {
                        if (interactor.HasInteractable)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public bool HasSelectedInteractable => State == InteractorState.Select &&
                                               _selectInteractor.HasSelectedInteractable;

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

        public virtual void AddInteractor(IInteractor interactor)
        {
            Interactors.Add(interactor);
            interactor.IsRootDriver = false;

            MonoBehaviour interactorMono = interactor as MonoBehaviour;
            if (interactorMono != null)
            {
                _interactors.Add(interactor as MonoBehaviour);
            }
        }

        public virtual void RemoveInteractor(IInteractor interactor)
        {
            if (!Interactors.Remove(interactor))
            {
                return;
            }
            interactor.IsRootDriver = true;

            MonoBehaviour interactorMono = interactor as MonoBehaviour;
            if (interactorMono != null)
            {
                _interactors.Remove(interactor as MonoBehaviour);
            }
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

                int result = CandidateComparer.Compare(a.CandidateProperties, b.CandidateProperties);
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

            Drive();
        }

        public void Drive()
        {
            Preprocess();

            InteractorState previousState = State;
            for (int i = 0; i < MaxIterationsPerFrame; i++)
            {
                if (State == InteractorState.Normal ||
                    (State == InteractorState.Hover && previousState != InteractorState.Normal))
                {
                    ProcessCandidate();
                }

                previousState = State;
                Process();

                if (State == InteractorState.Disabled)
                {
                    break;
                }

                if (State == InteractorState.Normal || State == InteractorState.Hover)
                {
                    Enable();
                }

                if(State == InteractorState.Normal)
                {
                    if (ShouldHover)
                    {
                        Hover();
                        continue;
                    }
                    break;
                }

                if (State == InteractorState.Hover)
                {
                    if (ShouldSelect)
                    {
                        Select();
                        continue;
                    }
                    if (ShouldUnhover)
                    {
                        Unhover();
                        continue;
                    }
                    break;
                }

                if(State == InteractorState.Select)
                {
                    if (ShouldUnselect)
                    {
                        Unselect();
                        continue;
                    }
                    break;
                }
            }

            Postprocess();
        }

        #region Inject

        public void InjectAllInteractorGroupMulti(List<IInteractor> interactors)
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
