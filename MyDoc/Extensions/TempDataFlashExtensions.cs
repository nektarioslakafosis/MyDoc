using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MyDoc.Extensions
{
    public static class TempDataFlashExtensions
    {
        private const string MessageKey = "FlashMessage";
        private const string TypeKey = "FlashMessageType";

        public static void FlashSuccess(
            this ITempDataDictionary tempData,
            string message)
        {
            SetFlashMessage(tempData, message, "success");
        }

        public static void FlashWarning(
            this ITempDataDictionary tempData,
            string message)
        {
            SetFlashMessage(tempData, message, "warning");
        }

        public static void FlashDanger(
            this ITempDataDictionary tempData,
            string message)
        {
            SetFlashMessage(tempData, message, "danger");
        }

        public static void FlashInfo(
            this ITempDataDictionary tempData,
            string message)
        {
            SetFlashMessage(tempData, message, "info");
        }

        private static void SetFlashMessage(
            ITempDataDictionary tempData,
            string message,
            string type)
        {
            tempData[MessageKey] = message;
            tempData[TypeKey] = type;
        }
    }
}