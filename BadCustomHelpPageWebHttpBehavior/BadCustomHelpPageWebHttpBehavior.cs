using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace BadCustomHelpPageWebHttpBehavior
{
    /// <summary>
    /// NOT INDENDED TO USE IN REAL ENVIRONMENT!
    /// Added just for demonstration
    /// </summary>
    public class BadCustomHelpPageWebHttpBehavior : WebHttpBehavior
    {
        /// <summary>
        /// Creates BadCustomHelpPageWebHttpBehavior
        /// </summary>
        /// <param name="ignoredMethodNames">Array of methods names to ignore in Help Page</param>
        public BadCustomHelpPageWebHttpBehavior(string[] ignoredMethodNames)
        {
            m_ignoredMethodNames = ignoredMethodNames;
        }

        /// <summary>
        /// Remove methods to display in Help Page by names passed in the constructor
        /// </summary>
        public override void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            base.ApplyDispatchBehavior(endpoint, endpointDispatcher);

            if (m_ignoredMethodNames == null || m_ignoredMethodNames.Length == 0)
                return;

            DispatchOperation helpOperation = endpointDispatcher.DispatchRuntime.Operations.FirstOrDefault(o => o.Name == "HelpPageInvoke");
            if(helpOperation == null)
                return;

            IOperationInvoker helpInvoker = helpOperation.Invoker;

            Type helpInvokerType = CreateInternalSystemServiceWebType("System.ServiceModel.Web.HelpOperationInvoker");
            FieldInfo helpPageFieldInfo = helpInvokerType.GetField("helpPage",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (helpPageFieldInfo != null)
            {
                object helpPage = helpPageFieldInfo.GetValue(helpInvoker);

                Type helpPageType = CreateInternalSystemServiceWebType("System.ServiceModel.Dispatcher.HelpPage");
                Type operationHelpInformationType =
                    CreateInternalSystemServiceWebType("System.ServiceModel.Dispatcher.OperationHelpInformation");

                Type dictionaryType = typeof (Dictionary<,>);
                Type[] operationInfoDictionaryGenericTypes = {typeof (string), operationHelpInformationType};
                Type operationInfoDictionaryType = dictionaryType.MakeGenericType(operationInfoDictionaryGenericTypes);

                FieldInfo operationInfoDictionaryFieldInfo = helpPageType.GetField("operationInfoDictionary",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (operationInfoDictionaryFieldInfo != null)
                {
                    object operationInfoDictionary = operationInfoDictionaryFieldInfo.GetValue(helpPage);
                    object operationInfoDictionaryReplaced = RemoveHelpMethods(operationInfoDictionary,
                        operationInfoDictionaryType);
                    operationInfoDictionaryFieldInfo.SetValue(helpPage, operationInfoDictionaryReplaced);
                }
            }
        }

        private object RemoveHelpMethods(object operationInfoDictionary, Type operationInfoDictionaryType)
        {
            Debug.Assert(m_ignoredMethodNames != null);

            var operationInfoDictionaryReplaced = Activator.CreateInstance(operationInfoDictionaryType);

            var operationInfoDictionaryAsEnumerable = operationInfoDictionary as IEnumerable;
            if (operationInfoDictionaryAsEnumerable != null)
            {
                foreach (var operationInfoEntry in operationInfoDictionaryAsEnumerable)
                {
                    object key = operationInfoEntry.GetType().GetProperty("Key").GetValue(operationInfoEntry);
                    object value = operationInfoEntry.GetType().GetProperty("Value").GetValue(operationInfoEntry);

                    string name = value.GetType().GetProperty("Name").GetValue(value) as string;

                    if (m_ignoredMethodNames.Contains(name) == false)
                    {
                        operationInfoDictionaryReplaced.GetType()
                            .GetMethod("Add")
                            .Invoke(operationInfoDictionaryReplaced, new[] {key, value});
                    }
                }
            }

            return operationInfoDictionaryReplaced;
        }

        private static Type CreateInternalSystemServiceWebType(string requestedType)
        {
            return typeof (WebServiceHost).Assembly.GetType(requestedType);
        }

        private readonly string[] m_ignoredMethodNames;
    }
}