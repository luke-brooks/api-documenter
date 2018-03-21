using System;
using System.Collections.Generic;
using ApiDocumentor.Models;
using System.Linq;

namespace ApiDocumentor.PageModel
{
    public class PageModel
    {
        public string ApiId { get; set; }
        public List<ApiControllerInformation> ApiInfo { get; set; }
        public ApiControllerInformation CurrentApi { get; set; }
        public MethodInformation CurrentMethod { get; set; }

        public PageModel(List<ApiControllerInformation> apiInformation, string apiId, string controllerName = "", string methodName = "", string attributeId = "") 
        {
            ApiId = apiId;
            ApiInfo = apiInformation;
            CurrentMethod = new MethodInformation();

            if (!String.IsNullOrEmpty(controllerName))
            {
                CurrentApi = ApiInfo.Where(x => x.ControllerName == controllerName).FirstOrDefault();

                if (!String.IsNullOrEmpty(methodName))
                {
                    CurrentMethod = CurrentApi.Methods.Where(x => x.Name == methodName && x.Attributes.Contains(attributeId)).FirstOrDefault();
                }
                else
                {
                    CurrentMethod = new MethodInformation { Name = "METHOD NOT FOUND" };
                }
            }
            else
            {
                CurrentApi = new ApiControllerInformation();
            }
        }
    }
}