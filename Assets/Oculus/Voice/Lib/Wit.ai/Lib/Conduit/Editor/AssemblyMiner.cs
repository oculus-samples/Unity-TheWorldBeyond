/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Mines assemblies for callback methods and entities.
    /// </summary>
    internal class AssemblyMiner : IAssemblyMiner
    {
        /// <summary>
        /// Validates that parameters are compatible. 
        /// </summary>
        private readonly IParameterValidator _parameterValidator;

        /// <summary>
        /// Set to true once the miner is initialized. No interactions with the class should be allowed before then.
        /// </summary>
        private bool _initialized = false;
        
        /// <inheritdoc/>
        public Dictionary<string, int> SignatureFrequency { get; private set; } = new Dictionary<string, int>();
        
        /// <inheritdoc/>
        public Dictionary<string, int> IncompatibleSignatureFrequency { get; private set; } = new Dictionary<string, int>();

        /// <summary>
        /// Initializes the class with a target assembly.
        /// </summary>
        /// <param name="parameterValidator">The parameter validator.</param>
        public AssemblyMiner(IParameterValidator parameterValidator)
        {
            this._parameterValidator = parameterValidator;
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            SignatureFrequency = new Dictionary<string, int>();
            IncompatibleSignatureFrequency = new Dictionary<string, int>();
            _initialized = true;
        }

        /// <inheritdoc/>
        public List<ManifestEntity> ExtractEntities(IConduitAssembly assembly)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Assembly Miner not initialized");
            }
            
            var entities = new List<ManifestEntity>();

            var enums = assembly.GetEnumTypes();
            foreach (var enumType in enums)
            {
                var enumUnderlyingType = Enum.GetUnderlyingType(enumType);
                Debug.Log(enumType.Name);
                Array enumValues;
                try
                {
                    if (enumType.GetCustomAttributes(typeof(ConduitEntityAttribute), false).Length == 0)
                    {
                        // This is not a tagged entity.
                        // TODO: In these cases we should only include the enum if it's referenced by any of the actions.
                    }
                    
                    enumValues = enumType.GetEnumValues();
                }
                catch (Exception e)
                {
                    Debug.Log($"Failed to get enumeration values. {e}");
                    continue;
                }
                
                var entity = new ManifestEntity
                {
                    ID = $"{enumType.Name}",
                    Type = "Enum",
                    Name = $"{enumType.Name}"
                };

                var values = new List<string>();
                
                foreach (var enumValue in enumValues)
                {
                    object underlyingValue = Convert.ChangeType(enumValue, enumUnderlyingType);
                    Debug.Log($"{enumValue} = {underlyingValue}");
                    values.Add(enumValue.ToString() ?? string.Empty);
                }

                entity.Values = values;
                entities.Add(entity);
            }

            return entities;
        }
        
        /// <inheritdoc/>
        public List<ManifestAction> ExtractActions(IConduitAssembly assembly)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Assembly Miner not initialized");
            }
            
            var methods = assembly.GetMethods();

            var actions = new List<ManifestAction>();

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(ConduitActionAttribute), false);
                if (attributes.Length == 0)
                {
                    continue;
                }
                
                var actionAttribute = attributes.First() as ConduitActionAttribute;
                var actionName = actionAttribute.Intent;
                if (string.IsNullOrEmpty(actionName))
                {
                    actionName = $"{method.Name}";
                }

                var parameters = new List<ManifestParameter>();

                var action = new ManifestAction()
                {
                    ID = $"{method.DeclaringType.FullName}.{method.Name}",
                    Name = actionName,
                    Assembly = assembly.FullName
                };

                var compatibleParameters = true;

                var signature = GetMethodSignature(method);
                
                // We track this first regardless of whether or not Conduit supports it to identify gaps.
                SignatureFrequency.TryGetValue(signature, out var currentFrequency); 
                SignatureFrequency[signature] = currentFrequency + 1;

                foreach (var parameter in method.GetParameters())
                {
                    var supported = _parameterValidator.IsSupportedParameterType(parameter.ParameterType);

                    if (!supported)
                    {
                        compatibleParameters = false;
                        continue;
                    }
                    
                    List<string> aliases;

                    if (parameter.GetCustomAttributes(typeof(ConduitParameterAttribute), false).Length > 0)
                    {
                        var parameterAttribute =
                            parameter.GetCustomAttributes(typeof(ConduitParameterAttribute), false).First() as
                                ConduitParameterAttribute;
                        aliases = parameterAttribute.Aliases;
                    }
                    else
                    {
                        aliases = new List<string>();
                    }

                    var snakeCaseName= ConduitUtilities.DelimitWithUnderscores(parameter.Name).ToLower().TrimStart('_');
                    var snakeCaseAction = action.ID.Replace('.', '_');
                    
                    var manifestParameter = new ManifestParameter
                    {
                        Name = parameter.Name,
                        InternalName = parameter.Name,
                        QualifiedTypeName = parameter.ParameterType.FullName,
                        TypeAssembly = parameter.ParameterType.Assembly.FullName,
                        Aliases = aliases,
                        QualifiedName = $"{snakeCaseAction}_{snakeCaseName}"
                    };

                    parameters.Add(manifestParameter);
                }

                if (compatibleParameters)
                {
                    action.Parameters = parameters;
                    actions.Add(action);
                }
                else
                {
                    Debug.Log($"{method} has Conduit-incompatible parameters");
                    IncompatibleSignatureFrequency.TryGetValue(signature, out currentFrequency); 
                    IncompatibleSignatureFrequency[signature] = currentFrequency + 1;
                }
            }

            return actions;
        }

        /// <summary>
        /// Generate a method signature summary that ignores method and parameter names but keeps types.
        /// For example:
        /// string F(int a, int b, float c) => string!int:2,float:1
        /// static string F(int a, int b, float c) => #string!int:2,float:1  
        /// </summary>
        /// <param name="methodInfo">The method we are capturing.</param>
        /// <returns>A string representing the relevant data types.</returns>
        private string GetMethodSignature(MethodInfo methodInfo)
        {
            var sb = new StringBuilder();
            if (methodInfo.IsStatic)
            {
                sb.Append('#');
            }
            
            sb.Append(methodInfo.ReturnType);
            sb.Append('!');
            var parameters = new SortedDictionary<string, int>();
            foreach (var parameter in methodInfo.GetParameters())
            {
                parameters.TryGetValue(parameter.ParameterType.Name, out var currentFrequency); 
                parameters[parameter.ParameterType.Name] = currentFrequency + 1;
            }

            var first = true;
            foreach (var parameter in parameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(',');
                }
                
                sb.Append(parameter.Key);
                sb.Append(':');
                sb.Append(parameter.Value);
            }

            return sb.ToString();
        }
    }
}
