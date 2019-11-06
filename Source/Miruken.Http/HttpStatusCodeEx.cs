#if NETFULL
namespace Miruken.Http
{
    using System.Net;

    public class HttpStatusCodeEx
    {
        public const int UnprocessableEntityCode = 422;

        public static HttpStatusCodeEx UnprocessableEntity
            = new HttpStatusCodeEx(UnprocessableEntityCode);

        private HttpStatusCodeEx(int code)
        {
            Code = code;
        }

        public int Code { get; }

        public static implicit operator HttpStatusCode(HttpStatusCodeEx extension)
        {
            return (HttpStatusCode)extension.Code;
        }
    }
}
#endif
