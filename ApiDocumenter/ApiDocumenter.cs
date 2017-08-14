using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Reflection;

namespace ApiDocumenter
{
    public static class Documenter
    {
        private const string START_LIST = "[";
        private const string END_LIST = "]";
        private const string START_OBJECT = "{\n";
        private const string END_OBJECT = "}";

        private const string LINE_BREAK = "\n";
        private const string TAB = "\t";
        private const string COMMA = ",";

        private const string STR_PROP = "\"{0}\": \"\"";
        private const string NUM_PROP = "\"{0}\": 0";
        private const string EMPTY_LIST_PROP = "\"{0}\": []";
        private const string FILLED_LIST_PROP = "\"{0}\": [\n";

        private static int _jsonBuildCurrentCall = 0;

        public static List<ApiControllerInformation> CreateApiDocumentation(List<Type> types, Type controllerParentType)
        {
            var controllerInfo = new List<ApiControllerInformation>();

            foreach (var t in types)
            {
                if (t.IsSubclassOf(controllerParentType))
                {
                    var tempInfo = new ApiControllerInformation()
                    {
                        ControllerName = t.Name,
                        Environment = "NEED ENVIRONMENT SPECIFIC INFO"
                    };

                    tempInfo.Methods = ShowControllerMethods(t.UnderlyingSystemType);

                    controllerInfo.Add(tempInfo);
                }
            }

            return controllerInfo;
        }

        private static List<MethodInformation> ShowControllerMethods(Type type)
        {
            var methodList = new List<MethodInformation>();

            foreach (var method in type.GetMethods())
            {
                if (!IsGenericMethod(method.Name))
                {
                    var tempMethod = new MethodInformation()
                    {
                        Name = method.Name,
                        ReturnType = BuildTypeInformation(method.ReturnType)
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

            if (type.Equals(typeof(string)))
            {
                return false;
            }
            if (type.Equals(typeof(decimal)))
            {
                return false;
            }
            if (type.Equals(typeof(DateTime)))
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

            _jsonBuildCurrentCall = 0;

            var jsonObj = CreateStringObject(complexProps, builder);

            return jsonObj;
        }

        private static string CreateStringObject(List<TypeInformation> complexProps, StringBuilder builder)
        {
            builder.Append(TAB.Multiply(_jsonBuildCurrentCall));
            builder.Append(START_OBJECT);

            var count = 1;
            foreach (var prop in complexProps)
            {
                builder.Append(TAB);
                builder.Append(TAB.Multiply(_jsonBuildCurrentCall));

                if (prop.Type.Contains("System.Int") || prop.Type.Contains("System.Decimal"))
                {
                    builder.Append(TAB.Multiply(_jsonBuildCurrentCall));
                    builder.Append(String.Format(NUM_PROP, prop.Name));
                }
                else if (prop.Type.Contains("System.Collections.Generic"))
                {
                    if (prop.ComplexProperties.Count > 0)
                    {
                        builder.Append(TAB.Multiply(_jsonBuildCurrentCall));
                        builder.Append(String.Format(FILLED_LIST_PROP, prop.Name));
                        builder.Append(TAB);
                        builder.Append(TAB);

                        _jsonBuildCurrentCall++;

                        var innerObj = CreateStringObject(prop.ComplexProperties, new StringBuilder());

                        _jsonBuildCurrentCall--;

                        int index = innerObj.LastIndexOf("}");
                        innerObj = innerObj.Insert(index, TAB + TAB);

                        builder.Append(innerObj);
                        builder.Append(LINE_BREAK);
                        builder.Append(TAB);
                        builder.Append(TAB.Multiply(_jsonBuildCurrentCall));
                        builder.Append(END_LIST);
                    }
                    else
                    {
                        builder.Append(TAB.Multiply(_jsonBuildCurrentCall));
                        builder.Append(EMPTY_LIST_PROP);
                    }
                }
                else
                {
                    builder.Append(TAB.Multiply(_jsonBuildCurrentCall));
                    builder.Append(String.Format(STR_PROP, prop.Name));
                }

                if (count != complexProps.Count)
                {
                    builder.Append(COMMA);
                }

                builder.Append(LINE_BREAK);

                count++;
            }

            builder.Append(TAB.Multiply(_jsonBuildCurrentCall));
            builder.Append(END_OBJECT);

            return builder.ToString();
        }

        private static string Multiply(this string source, int multiplier)
        {
            StringBuilder sb = new StringBuilder(multiplier * source.Length);
            for (int i = 0; i < multiplier; i++)
            {
                sb.Append(source);
            }

            return sb.ToString();
        }
    }
}