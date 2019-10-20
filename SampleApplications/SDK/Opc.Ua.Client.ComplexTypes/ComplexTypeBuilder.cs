/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/


using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace Opc.Ua.Client.ComplexTypes
{
    /// <summary>
    /// 
    /// </summary>
    public class ComplexTypeBuilder
    {
        #region Constructors
        /// <summary>
        /// Initializes the object with default values.
        /// </summary>
        public ComplexTypeBuilder(
            string targetNamespace,
            string moduleName = null,
            string assemblyName = null)
        {
            m_targetNamespace = targetNamespace;
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName ?? Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.GetDynamicModule(moduleName ?? m_opcTypesModuleName);
            if (moduleBuilder == null)
            {
                moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName ?? m_opcTypesModuleName);
            }
            m_moduleBuilder = moduleBuilder;
        }
        #endregion

        #region Public Properties
        public Type AddEnumType(Schema.Binary.EnumeratedType enumeratedType)
        {
            if (enumeratedType == null)
            {
                throw new ArgumentNullException(nameof(enumeratedType));
            }
            var enumBuilder = m_moduleBuilder.DefineEnum(enumeratedType.Name, TypeAttributes.Public, typeof(int));
            enumBuilder.DataContractAttribute(m_targetNamespace);
            foreach (var enumValue in enumeratedType.EnumeratedValue)
            {
                var newEnum = enumBuilder.DefineLiteral(enumValue.Name, enumValue.Value);
                newEnum.EnumAttribute(enumValue.Name, enumValue.Value);
            }
            return enumBuilder.CreateTypeInfo();
        }

        public ComplexTypeFieldBuilder AddStructuredType(
            Schema.Binary.StructuredType structuredType,
            NodeId typeId)
        {
            var structureBuilder = m_moduleBuilder.DefineType(structuredType.Name, TypeAttributes.Public | TypeAttributes.Class, typeof(BaseComplexType));
            structureBuilder.DataContractAttribute(m_targetNamespace);
            var structureDefinition = new StructureDefinition()
            {
                DefaultEncodingId = typeId,
                BaseDataType = NodeId.Null,
                StructureType = StructureType.Structure
            };
            structureBuilder.StructureDefinitonAttribute(structureDefinition);
            return new ComplexTypeFieldBuilder(structureBuilder);
        }
        #endregion

        #region Private Members
        #endregion

        #region Private Fields
        private const string m_opcTypesModuleName = "Opc.Ua.ComplexType.Assembly";
        private ModuleBuilder m_moduleBuilder;
        private string m_targetNamespace;
        #endregion
    }

    /// <summary>
    /// Helper to build property fields.
    /// </summary>
    public class ComplexTypeFieldBuilder
    {
        #region Constructors
        public ComplexTypeFieldBuilder(TypeBuilder structureBuilder)
        {
            m_structureBuilder = structureBuilder;
        }
        #endregion

        #region Public Properties
        public void AddField(string fieldName, Type fieldType, int order)
        {
            var fieldBuilder = m_structureBuilder.DefineField("_" + fieldName, fieldType, FieldAttributes.Private);
            var propertyBuilder = m_structureBuilder.DefineProperty(fieldName, PropertyAttributes.None, fieldType, null);
            var methodAttributes =
                System.Reflection.MethodAttributes.Public |
                System.Reflection.MethodAttributes.HideBySig |
                System.Reflection.MethodAttributes.Virtual;

            var setBuilder = m_structureBuilder.DefineMethod("set_" + fieldName, methodAttributes, null, new[] { fieldType });
            var setIl = setBuilder.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            setIl.Emit(OpCodes.Ret);

            var getBuilder = m_structureBuilder.DefineMethod("get_" + fieldName, methodAttributes, fieldType, Type.EmptyTypes);
            var getIl = getBuilder.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getBuilder);
            propertyBuilder.SetSetMethod(setBuilder);
            propertyBuilder.DataMemberAttribute(fieldName, false, order);
        }

        public Type CreateType()
        {
            var complexType = m_structureBuilder.CreateType();
            m_structureBuilder = null;
            return complexType;
        }
        #endregion

        #region Private Fields
        private TypeBuilder m_structureBuilder;
        #endregion
    }
}//namespace
