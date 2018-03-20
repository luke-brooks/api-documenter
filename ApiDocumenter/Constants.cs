namespace ApiDocumenter
{
    class Constants
    {
        #region JSON Object Strings

        public const string START_LIST = "[";
        public const string END_LIST = "]";
        public const string START_OBJECT = "{\n";
        public const string END_OBJECT = "}";

        public const string LINE_BREAK = "\n";
        public const string COMMA = ",";

        public const string STR_PROP = "\"{0}\": \"\"";
        public const string NUM_PROP = "\"{0}\": 0";
        public const string COMPLEX_PROP = "\"{0}\": ";
        public const string EMPTY_LIST_PROP = "\"{0}\": []";
        public const string FILLED_LIST_PROP = "\"{0}\": [\n";

        #endregion

        #region Attributes

        public const string GET = "GET";
        public const string POST = "POST";
        public const string PUT = "PUT";
        public const string DELETE = "DELETE";

        #endregion
    }
}
