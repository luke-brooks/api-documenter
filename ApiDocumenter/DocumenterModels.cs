using System.Collections.Generic;

namespace ApiDocumenter
{
    public class ApiControllerInformation
    {
        public string ControllerName { get; set; }
        public string Environment { get; set; }
        public List<MethodInformation> Methods { get; set; }

        public ApiControllerInformation()
        {
            Methods = new List<MethodInformation>();
        }
    }

    public class MethodInformation
    {
        public string Name { get; set; }
        public List<TypeInformation> Parameters { get; set; }
        public TypeInformation ReturnType { get; set; }

        public MethodInformation()
        {
            Parameters = new List<TypeInformation>();
        }
    }

    public class TypeInformation
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<TypeInformation> ComplexProperties { get; set; }
        public string JsonObject { get; set; }

        public TypeInformation()
        {
            ComplexProperties = new List<TypeInformation>();
        }
    }
}
