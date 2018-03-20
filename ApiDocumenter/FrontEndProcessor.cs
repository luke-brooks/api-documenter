using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ApiDocumenter.Models;

namespace ApiDocumenter
{
    public static partial class ApiDocumentationProcessor
    {
        public static List<ApiControllerInformation> FormatApiDataForDisplay(List<ApiControllerInformation> apiInfo)
        {
            foreach (var api in apiInfo)
            {
                api.ControllerName = api.ControllerName.Replace("Controller", String.Empty);

                foreach (var method in api.Methods)
                {
                    method.ReturnType.Type = ProcessType(method.ReturnType.Type);
                    method.ReturnType.ComplexProperties = ProcessComplexProperties(method.ReturnType.ComplexProperties);


                    foreach (var param in method.Parameters)
                    {
                        param.Type = ProcessType(param.Type);
                        param.ComplexProperties = ProcessComplexProperties(param.ComplexProperties);
                    }
                }
            }

            return apiInfo;
        }

        private static List<TypeInformation> ProcessComplexProperties(List<TypeInformation> complexProperties)
        {
            var list = complexProperties.Select(x => new TypeInformation
            {
                Name = x.Name,
                Type = ProcessType(x.Type),
                ComplexProperties = x.ComplexProperties.Count > 0 ? ProcessComplexProperties(x.ComplexProperties) : new List<TypeInformation>()
            }).ToList();

            return list;
        }

        private static string ProcessType(string type)
        {
            if (type.StartsWith("System.Nullable") || type.StartsWith("System.Collections.Generic"))
            {
                type = GetAlteredTypeFormat(type);
            }
            else
            {
                type = GetSystemTypeFormat(type);
            }

            return type;
        }

        private static string GetAlteredTypeFormat(string systemRaw)
        {
            var altIndex = systemRaw.IndexOf('`');
            var altOuterInfo = systemRaw.Substring(0, altIndex);
            var altOuterType = altOuterInfo.Split('.').ToList().Last();

            var altInnerInfo = Regex.Match(systemRaw, @"\[\[([^]]*)\]\]").Groups[1].Value;
            var altInnerType = GetSystemTypeFormat(altInnerInfo.Split(',').ToList().First());

            var formatStr = altOuterType == "Nullable" ? "{1}?" : "{0}<{1}>";

            var result = String.Format(formatStr, altOuterType, altInnerType);

            return result;
        }

        private static string GetSystemTypeFormat(string systemType)
        {
            return systemType.Split('.').ToList().Last();
        }
    }
}
