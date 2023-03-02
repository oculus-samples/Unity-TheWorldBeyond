/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// An UpdateDriverGroup updates a set of IUpdateDrivers a specified number of times per update.
    /// It acts as the root driver for a provided set of IUpdateDrivers.
    /// </summary>
    public class UpdateDriverGroup : MonoBehaviour, IUpdateDriver
    {
        public bool IsRootDriver { get; set; } = true;

        [SerializeField, Interface(typeof(IUpdateDriver))]
        private List<MonoBehaviour> _updateDrivers;
        protected List<IUpdateDriver> Drivers;

        [SerializeField]
        private int _iterations = 3;

        #region Properties

        public int Iterations {
            get
            {
                return _iterations;
            }
            set
            {
                _iterations = value;
            }
        }

        #endregion

        protected virtual void Awake()
        {
            Drivers = _updateDrivers.ConvertAll(mono => mono as IUpdateDriver);
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            foreach (IUpdateDriver driver in Drivers)
            {
                Assert.IsNotNull(driver);
            }

            Assert.IsTrue(_iterations > 0);
        }

        // Update is called once per frame
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
            for (int i = 0; i < _iterations; i++)
            {
                foreach (IUpdateDriver driver in Drivers)
                {
                    driver.Drive();
                }
            }
        }

        #region Inject

        public void InjectAllUpdateDriverGroup(List<IUpdateDriver> updateDrivers)
        {
            InjectUpdateDrivers(updateDrivers);
        }

        public void InjectUpdateDrivers(List<IUpdateDriver> updateDrivers)
        {
            Drivers = updateDrivers;
            _updateDrivers = updateDrivers.ConvertAll(driver => driver as MonoBehaviour);
        }

        #endregion
    }
}
