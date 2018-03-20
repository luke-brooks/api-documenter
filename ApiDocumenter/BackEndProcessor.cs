using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Reflection;
using ApiDocumenter.Models;

namespace ApiDocumenter
{
    public static partial class ApiDocumentationProcessor
    {

        public static List<ApiControllerInformation> CreateApiDocumentation(List<Type> types, Type controllerParentType)
        {
            var controllerInfo = new List<ApiControllerInformation>();

            foreach (var t in types)
            {
                if (t.IsSubclassOf(controllerParentType))
                {
                    var tempInfo = new ApiControllerInformation()
                    {
                        ControllerName = t.Name
                    };

                    tempInfo.Methods = ProcessControllerMethods(t.UnderlyingSystemType);

                    controllerInfo.Add(tempInfo);
                }
            }

            return controllerInfo;
        }

        private static List<MethodInformation> ProcessControllerMethods(Type type)
        {
            var methodList = new List<MethodInformation>();

            foreach (var method in type.GetMethods())
            {
                if (!IsGenericMethod(method.Name))
                {
                    var tempMethod = new MethodInformation()
                    {
                        Name = method.Name,
                        ReturnType = BuildTypeInformation(method.ReturnType),
                        Attributes = GetAttributeTags(method)
                    };

                    var parameters = method.GetParameters();
                    tempMethod.Parameters = parameters.Select(param => BuildTypeInformation(param)).ToList();

                    methodList.Add(tempMethod);
                }
            }

            return methodList;
        }

        private static bool IsGenericMethod(string methodName)
        {
            bool isGeneric = false;
            List<string> genericMethods = new List<string>
            {
                "Dispose",
                "ToString",
                "Equals",
                "GetHashCode",
                "GetType",
                "ExecuteAsync",
                "get_User",
                "set_Url",
                "get_Url",
                "get_ModelState",
                "set_ControllerContext",
                "get_ControllerContext",
                "set_Configuration",
                "get_Configuration",
                "set_Request",
                "get_Request"
            };

            foreach (var generic in genericMethods)
            {
                if (methodName.ToUpper() == generic.ToUpper()) { isGeneric = true; }
            }

            return isGeneric;
        }

        private static List<string> GetAttributeTags(MethodInfo method)
        {
            var attributes = new List<string>();

            foreach (var a in method.CustomAttributes)
            {
                if (a.AttributeType == typeof(System.Web.Http.HttpGetAttribute) ||
                        a.AttributeType == typeof(System.Web.Mvc.HttpGetAttribute))
                {
                    attributes.Add(Constants.GET);
                }

                if (a.AttributeType == typeof(System.Web.Http.HttpPostAttribute) ||
                        a.AttributeType == typeof(System.Web.Mvc.HttpPostAttribute))
                {
                    attributes.Add(Constants.POST);
                }

                if (a.AttributeType == typeof(System.Web.Http.HttpPutAttribute) ||
                        a.AttributeType == typeof(System.Web.Mvc.HttpPutAttribute))
                {
                    attributes.Add(Constants.PUT);
                }

                if (a.AttributeType == typeof(System.Web.Http.HttpDeleteAttribute) ||
                        a.AttributeType == typeof(System.Web.Mvc.HttpDeleteAttribute))
                {
                    attributes.Add(Constants.DELETE);
                }
            }

            return attributes;
        }

        private static TypeInformation BuildTypeInformation(Type typeToBuild)
        {
            var typeInfo = new TypeInformation();

            typeInfo.Type = typeToBuild.FullName;
            typeInfo.ComplexProperties = GetComplexProperties(typeToBuild);
            typeInfo.JsonObject = IsComplexObject(typeToBuild) ?
                                    BuildJsonString(typeInfo.ComplexProperties) :
                                        String.Empty;

            return typeInfo;
        }

        private static TypeInformation BuildTypeInformation(ParameterInfo paramToBuild)
        {
            var typeInfo = new TypeInformation();

            typeInfo.Name = paramToBuild.Name;
            typeInfo.Type = paramToBuild.ParameterType.FullName;
            typeInfo.ComplexProperties = GetComplexProperties(paramToBuild.ParameterType);
            typeInfo.JsonObject = IsComplexObject(paramToBuild.ParameterType) ?
                                        BuildJsonString(typeInfo.ComplexProperties) :
                                            String.Empty;

            return typeInfo;
        }

        private static TypeInformation BuildTypeInformation(PropertyInfo propToBuild)
        {
            var typeInfo = new TypeInformation();

            typeInfo.Name = propToBuild.Name;
            typeInfo.Type = propToBuild.PropertyType.FullName;
            typeInfo.ComplexProperties = IsComplexObject(propToBuild.PropertyType) ?
                                                GetComplexProperties(propToBuild.PropertyType) :
                                                    new List<TypeInformation>();
            typeInfo.JsonObject = IsComplexObject(propToBuild.PropertyType) ?
                                        BuildJsonString(typeInfo.ComplexProperties) :
                                            String.Empty;

            return typeInfo;
        }

        private static List<TypeInformation> GetComplexProperties(Type complexType)
        {
            var complexProperites = new List<TypeInformation>();

            if (IsList(complexType))
            {
                complexProperites = GetComplexProperties(complexType.GetGenericArguments()[0]);
                return complexProperites;
            }

            if (complexType.Equals(typeof(HttpResponseMessage)))
            {
                return complexProperites;
            }

            if (IsRecursiveObject(complexType))
            {
                complexProperites = BuildRecursivePropertyList(complexType);
                return complexProperites;
            }

            if (IsComplexObject(complexType))
            {
                complexProperites = BuildComplexPropertyList(complexType);
            }

            return complexProperites;
        }

        private static bool IsList(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return true;
            }

            return false;
        }

        private static bool IsNullable(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return true;
            }

            return false;
        }

        private static bool IsRecursiveObject(Type type)
        {
            var propTypeList = type.GetProperties().Select(x => x.PropertyType).ToList();

            foreach (var propType in propTypeList)
            {
                if (IsList(propType))
                {
                    var innerType = propType.GetGenericArguments()[0];
                    if (type.Equals(innerType))
                    {
                        return true;
                    }
                }

                if (type.Equals(propType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsComplexObject(Type type)
        {
            if (IsNullable(type))
            {
                // nullable type, check if the nested type is simple
                return IsComplexObject(type.GetGenericArguments()[0]);
            }
            if (type.IsArray)
            {
                return IsComplexObject(type.GetElementType());
            }

            if (type.Equals(typeof(string)))
            {
                return false;
            }
            if (type.Equals(typeof(decimal)))
            {
                return false;
            }
            if (type.Equals(typeof(DataTable)))
            {
                return false;
            }
            if (type.IsValueType)
            {
                return false;
            }
            if (type.IsPrimitive)
            {
                return false;
            }
            if (type.IsEnum)
            {
                return false;
            }

            return true;
        }

        private static List<TypeInformation> BuildRecursivePropertyList(Type complexType)
        {
            var returnList = new List<TypeInformation>();

            var complexPropList = complexType.GetProperties().ToList();

            foreach (var prop in complexPropList)
            {
                var propType = prop.PropertyType;

                var tempProp = new TypeInformation
                {
                    Name = prop.Name,
                    Type = propType.FullName
                };

                if (IsList(propType))
                {
                    propType = propType.GetGenericArguments()[0];
                }

                if (IsRecursiveObject(propType))
                {
                    tempProp.ComplexProperties = new List<TypeInformation>();
                }
                else
                {
                    tempProp.ComplexProperties = IsComplexObject(propType) ?
                                                    GetComplexProperties(propType) :
                                                        new List<TypeInformation>();
                }

                tempProp.JsonObject = IsComplexObject(propType) ?
                                            BuildJsonString(tempProp.ComplexProperties) :
                                                String.Empty;

                returnList.Add(tempProp);
            }

            return returnList;
        }

        private static List<TypeInformation> BuildComplexPropertyList(Type type)
        {
            var list = type.GetProperties().Select(prop => BuildTypeInformation(prop)).ToList();

            return list;
        }

        private static string BuildJsonString(List<TypeInformation> complexProps)
        {
            var builder = new StringBuilder();

            var jsonObj = CreateStringObject(complexProps, builder);

            return jsonObj;
        }

        private static string CreateStringObject(List<TypeInformation> complexProps, StringBuilder builder)
        {
            builder.Append(Constants.START_OBJECT);

            var count = 1;
            foreach (var prop in complexProps)
            {
                if (prop.Type.Contains("System.Int") || prop.Type.Contains("System.Decimal"))
                {
                    builder.Append(String.Format(Constants.NUM_PROP, prop.Name));
                }
                else if (prop.Type.Contains("System.Collections.Generic"))
                {
                    if (prop.ComplexProperties.Count > 0)
                    {
                        builder.Append(String.Format(Constants.FILLED_LIST_PROP, prop.Name));

                        var innerObj = CreateStringObject(prop.ComplexProperties, new StringBuilder());

                        builder.Append(innerObj);
                        builder.Append(Constants.LINE_BREAK);
                        builder.Append(Constants.END_LIST);
                    }
                    else
                    {
                        builder.Append(String.Format(Constants.EMPTY_LIST_PROP, prop.Name));
                    }
                }
                else
                {
                    if (prop.ComplexProperties.Count > 0)
                    {
                        var innerObj = CreateStringObject(prop.ComplexProperties, new StringBuilder());

                        builder.Append(String.Format(Constants.COMPLEX_PROP, prop.Name));
                        builder.Append(innerObj);
                    }
                    else
                    {
                        builder.Append(String.Format(Constants.STR_PROP, prop.Name));
                    }
                }

                if (count != complexProps.Count)
                {
                    builder.Append(Constants.COMMA);
                }

                builder.Append(Constants.LINE_BREAK);

                count++;
            }

            builder.Append(Constants.END_OBJECT);

            return builder.ToString();
        }
    }
}
