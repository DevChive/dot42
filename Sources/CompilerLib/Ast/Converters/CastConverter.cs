﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dot42.CompilerLib.XModel;
using Dot42.FrameworkDefinitions;

namespace Dot42.CompilerLib.Ast.Converters
{
    /// <summary>
    /// Add/expand cast / instance of and IsAssignableFrom expressions
    /// - for Arrays
    /// - for IFormattable
    /// </summary>
    internal static class CastConverter
    {
        /// <summary>
        /// Optimize expressions
        /// </summary>
        public static void Convert(AstNode ast, MethodSource currentMethod, AssemblyCompiler compiler)
        {
            var typeSystem = compiler.Module.TypeSystem;

            // Expand instanceof
            foreach (var node in ast.GetSelfAndChildrenRecursive<AstExpression>(x => x.Code == AstCode.InstanceOf))
            {
                ConvertInstanceOf(compiler, node, typeSystem);
            }

            // Expand isinst
            foreach (var node in ast.GetSelfAndChildrenRecursive<AstExpression>(x => x.Code == AstCode.Isinst))
            {
                ConvertIsInst(compiler, node, typeSystem);
            }

            // Expand Castclass
            foreach (var node in ast.GetSelfAndChildrenRecursive<AstExpression>(x => x.Code == AstCode.Castclass))
            {
                ConvertCastclass(compiler, node, typeSystem);
            }

            foreach (var node in ast.GetSelfAndChildrenRecursive<AstExpression>(x => x.Code == AstCode.Callvirt))
            {
                ConvertCallvirtIEnumerable(compiler, node, typeSystem);
            }

            foreach (var node in ast.GetSelfAndChildrenRecursive<AstExpression>(x => x.Code == AstCode.Ret))
            {
                ConvertRet(compiler, node, typeSystem, currentMethod);
            }
        }

        /// <summary>
        /// Convert node with code Cast.
        /// </summary>
        private static void ConvertCastclass(AssemblyCompiler compiler, AstExpression node, XTypeSystem typeSystem)
        {
            var type = (XTypeReference) node.Operand;
            if (type.IsSystemArray())
            {
                // Call cast method
                var arrayHelper = compiler.GetDot42InternalType(InternalConstants.CompilerHelperName).Resolve();
                var castToArray = arrayHelper.Methods.First(x => x.Name == "CastToArray");
                var castToArrayExpr = new AstExpression(node.SourceLocation, AstCode.Call, castToArray, node.Arguments).SetType(type);
                node.CopyFrom(castToArrayExpr);
                return;
            }

            string castMethod = null;
            if (type.IsSystemCollectionsIEnumerable() ||
                type.IsSystemCollectionsIEnumerableT())
            {
                castMethod = "CastToEnumerable";
            }
            else if (type.IsSystemCollectionsICollection() ||
                     type.IsSystemCollectionsICollectionT())
            {
                castMethod = "CastToCollection";
            }
            else if (type.IsSystemCollectionsIList() ||
                     type.IsSystemCollectionsIListT())
            {
                castMethod = "CastToList";
            }
            else if (type.IsSystemIFormattable())
            {
                castMethod = "CastToFormattable";
            }

            if (castMethod != null)
            {
                // make sure we don't evaluate the expression twice.
                var tempVar = new AstGeneratedVariable("temp$$", "") { Type = compiler.Module.TypeSystem.Object };
                var storeTempVar = new AstExpression(node.SourceLocation, AstCode.Stloc, tempVar, node.Arguments[0]) { ExpectedType =compiler.Module.TypeSystem.Object} ;
                var loadTempVar = new AstExpression(node.SourceLocation, AstCode.Ldloc, tempVar).SetType(compiler.Module.TypeSystem.Object);

                // Call cast method
                var arrayHelper = compiler.GetDot42InternalType(InternalConstants.CompilerHelperName).Resolve();
                var castToArray = arrayHelper.Methods.First(x => x.Name == castMethod);

                // Call "(x instanceof T) ? (T)x : asMethod(x)"

                // "instanceof x"
                var instanceofExpr = new AstExpression(node.SourceLocation, AstCode.SimpleInstanceOf, type, storeTempVar).SetType(typeSystem.Bool);

                // CastX(x)
                var castXExpr = new AstExpression(node.SourceLocation, AstCode.Call, castToArray, loadTempVar).SetType(typeSystem.Object);

                // T(x)
                var txExpr = new AstExpression(node.SourceLocation, AstCode.SimpleCastclass, type, loadTempVar).SetType(type);

                // Combine
                var conditional = new AstExpression(node.SourceLocation, AstCode.Conditional, type, instanceofExpr, txExpr, castXExpr).SetType(type);

                node.CopyFrom(conditional);
                return;
            }

            // Normal castclass
            node.Code = AstCode.SimpleCastclass;
        }

        /// <summary>
        /// Convert node with code IsInst.
        /// </summary>
        private static void ConvertIsInst(AssemblyCompiler compiler, AstExpression node, XTypeSystem typeSystem)
        {
            var type = (XTypeReference)node.Operand;
            if (type.IsSystemArray())
            {
                // Call ArrayHelper.AsArray
                var arrayHelper = compiler.GetDot42InternalType(InternalConstants.CompilerHelperName).Resolve();
                var asArray = arrayHelper.Methods.First(x => x.Name == "AsArray");
                var asArrayExpr = new AstExpression(node.SourceLocation, AstCode.Call, asArray, node.Arguments).SetType(typeSystem.Bool);
                node.CopyFrom(asArrayExpr);
                return;
            }

            string asMethod = null;
            if (type.IsSystemCollectionsIEnumerable() ||
                type.IsSystemCollectionsIEnumerableT())
            {
                asMethod = "AsEnumerable";
            }
            else if (type.IsSystemCollectionsICollection()||
                     type.IsSystemCollectionsICollectionT())
            {
                asMethod = "AsCollection";
            }
            else if (type.IsSystemCollectionsIList() ||
                     type.IsSystemCollectionsIListT())
            {
                asMethod = "AsList";
            }
            else if (type.IsSystemIFormattable())
            {
                asMethod = "AsFormattable";
            }

            // make sure we don't evaluate the expression twice.
            var tempVar = new AstGeneratedVariable("temp$$", "") { Type = compiler.Module.TypeSystem.Object };
            var storeTempVar = new AstExpression(node.SourceLocation, AstCode.Stloc, tempVar, node.Arguments[0]) { ExpectedType = compiler.Module.TypeSystem.Object };
            var loadTempVar = new AstExpression(node.SourceLocation, AstCode.Ldloc, tempVar).SetType(compiler.Module.TypeSystem.Object);


            if (asMethod != null)
            {
                // Call "(x instanceof T) ? (T)x : asMethod(x)"
                var arrayHelper = compiler.GetDot42InternalType(InternalConstants.CompilerHelperName).Resolve();
                var asArray = arrayHelper.Methods.First(x => x.Name == asMethod);

                // "instanceof x"
                var instanceofExpr = new AstExpression(node.SourceLocation, AstCode.SimpleInstanceOf, type, storeTempVar).SetType(typeSystem.Bool);

                // AsX(x)
                var asXExpr = new AstExpression(node.SourceLocation, AstCode.Call, asArray, loadTempVar).SetType(typeSystem.Object);

                // T(x)
                var txExpr = new AstExpression(node.SourceLocation, AstCode.SimpleCastclass, type, loadTempVar).SetType(type);

                // Combine
                var conditional = new AstExpression(node.SourceLocation, AstCode.Conditional, type, instanceofExpr, txExpr, asXExpr).SetType(type);

                node.CopyFrom(conditional);
                return;
            }

            // Normal "as": Convert to (x instanceof T) ? T(x) : null
            {
                // "instanceof x"
                var instanceofExpr = new AstExpression(node.SourceLocation, AstCode.SimpleInstanceOf, type, storeTempVar).SetType(typeSystem.Bool);

                // T(x)
                var txExpr = new AstExpression(node.SourceLocation, AstCode.SimpleCastclass, type, loadTempVar).SetType(type);

                // null
                var nullExpr = new AstExpression(node.SourceLocation, AstCode.Ldnull, null).SetType(typeSystem.Object);

                // Combine
                var conditional = new AstExpression(node.SourceLocation, AstCode.Conditional, type, instanceofExpr, txExpr, nullExpr).SetType(type);

                node.CopyFrom(conditional);
                return;                
            }
        }


        /// <summary>
        /// Convert node with code Callvirt. 
        /// 
        /// for arrays: intercepts call to IEnumerable.IEnumerable_GetEnumerator generated 
        /// by foreach statements and swaps them out to System.Array.GetEnumerator.
        /// 
        /// This call will then at a later compilation stage be replaced with the final destination.
        /// </summary>
        private static void ConvertCallvirtIEnumerable(AssemblyCompiler compiler, AstExpression node, XTypeSystem typeSystem)
        {
            var targetMethodRef = ((XMethodReference)node.Operand);
            var targetMethodDefOrRef = targetMethodRef;
            
            if (!targetMethodDefOrRef.DeclaringType.IsSystemCollectionsIEnumerable())
                return;
            if (targetMethodDefOrRef.Name != "IEnumerable_GetEnumerator")
                return;

            if (node.Arguments.Count != 1) return;

            var argument = node.Arguments[0];
            
            if (!argument.InferredType.IsArray)
                return;
            
            // swap the call to System.Array
            var systemArray = compiler.GetDot42InternalType("System", "Array").Resolve();
            var getEnumerator = systemArray.Methods.First(x => x.Name == "GetEnumerator" && !x.IsStatic && x.Parameters.Count == 0);
            node.Operand = getEnumerator;
        }

        /// <summary>
        /// Convert node with code ret. 
        /// 
        /// converts to IEnumerable, ICollection or IList if return value 
        /// is currently an Array.
        /// </summary>
        private static void ConvertRet(AssemblyCompiler compiler, AstExpression node, XTypeSystem typeSystem, MethodSource currentMethod)
        {
            var type = currentMethod.Method.ReturnType;
            
            string asMethod = null;
            if (type.IsSystemCollectionsIEnumerable() ||
                type.IsSystemCollectionsIEnumerableT())
            {
                asMethod = "AsEnumerable";
            }
            else if (type.IsSystemCollectionsICollection() ||
                     type.IsSystemCollectionsICollectionT())
            {
                asMethod = "AsCollection";
            }
            else if (type.IsSystemCollectionsIList() ||
                     type.IsSystemCollectionsIListT())
            {
                asMethod = "AsList";
            }

            if (asMethod == null) 
                return;

            if (node.Arguments.Count != 1) return;

            var argument = node.Arguments[0];

            if (argument.InferredType == null || !argument.InferredType.IsArray)
                return;

            // Call "ret asMethod(x)"
            var arrayHelper = compiler.GetDot42InternalType(InternalConstants.CompilerHelperName).Resolve();
            var asArray = arrayHelper.Methods.First(x => x.Name == asMethod);

            // AsX(x)
            var asXExpr = new AstExpression(node.SourceLocation, AstCode.Call, asArray, node.Arguments[0]).SetType(typeSystem.Object);

            // replace argument.
            node.Arguments[0] = asXExpr;
        }

        /// <summary>
        /// Convert node with code InstanceOf.
        /// </summary>
        private static void ConvertInstanceOf(AssemblyCompiler compiler, AstExpression node, XTypeSystem typeSystem)
        {
            var type = (XTypeReference)node.Operand;
            if (type.IsSystemArray()) // "is System.Array"
            {
                // Call ArrayHelper.IsArray
                var arrayHelper = compiler.GetDot42InternalType(InternalConstants.CompilerHelperName).Resolve();
                var isArray = arrayHelper.Methods.First(x => x.Name == "IsArray");
                var isArrayExpr = new AstExpression(node.SourceLocation, AstCode.Call, isArray, node.Arguments).SetType(typeSystem.Bool);
                node.CopyFrom(isArrayExpr);
                return;
            }

            // make sure we don't evaluate the expression twice.
            var tempVar = new AstGeneratedVariable("temp$$", "") { Type = compiler.Module.TypeSystem.Object };
            var storeTempVar = new AstExpression(node.SourceLocation, AstCode.Stloc, tempVar, node.Arguments[0]) { ExpectedType = compiler.Module.TypeSystem.Object };
            var loadTempVar = new AstExpression(node.SourceLocation, AstCode.Ldloc, tempVar).SetType(compiler.Module.TypeSystem.Object);

            if (type.IsSystemCollectionsIEnumerable() ||
                type.IsSystemCollectionsICollection() ||
                type.IsSystemCollectionsIList())
            {
                // Call "(is x) || IsArray(x)"
                var arrayHelper = compiler.GetDot42InternalType(InternalConstants.CompilerHelperName).Resolve();
                var isArray = arrayHelper.Methods.First(x => x.Name == "IsArray" && x.Parameters.Count == 1);

                // "is" 
                var isExpr = new AstExpression(node).SetArguments(storeTempVar).SetCode(AstCode.SimpleInstanceOf);

                // Call IsArray
                var isArrayExpr = new AstExpression(node.SourceLocation, AstCode.Call, isArray, loadTempVar).SetType(typeSystem.Bool);

                // Combined
                var combined = new AstExpression(node.SourceLocation, AstCode.Or, null, isExpr, isArrayExpr).SetType(typeSystem.Bool);
                node.CopyFrom(combined);
                return;
            }

            if (type.IsSystemCollectionsIEnumerableT() ||
                            type.IsSystemCollectionsICollectionT() ||
                            type.IsSystemCollectionsIListT())
            {
                // TODO: implement InstanceOf with type check for array types.
                // (is that even possible here?)
            }

            if (type.IsSystemIFormattable())
            {
                // Call "(is x) || IsFormattable(x)"
                var formattable = compiler.GetDot42InternalType(InternalConstants.CompilerHelperName).Resolve();
                var isFormattable = formattable.Methods.First(x => x.Name == "IsVirtualFormattable");

                // "is" 
                var isExpr = new AstExpression(node).SetArguments(storeTempVar).SetCode(AstCode.SimpleInstanceOf);

                // Call IsFormattable
                var isFormattableExpr = new AstExpression(node.SourceLocation, AstCode.Call, isFormattable, loadTempVar).SetType(typeSystem.Bool);

                // Combined
                var combined = new AstExpression(node.SourceLocation, AstCode.Or, null, isExpr, isFormattableExpr).SetType(typeSystem.Bool);
                node.CopyFrom(combined);
                return;
            }

            // Normal instanceof
            node.Code = AstCode.SimpleInstanceOf;            
        }
    }
}
