using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Facebook.WitAi
{
    public interface IRequest
    {
        WebHeaderCollection Headers { get; set; }
        string Method { get; set; }
        string ContentType { get; set; }
        long ContentLength { get; set; }
        bool SendChunked { get; set; }
        string UserAgent { get; set; }
        int Timeout { get; set; }

        IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state);
        IAsyncResult BeginGetResponse(AsyncCallback callback, object state);
        /// <summary>
        /// Returns a Stream for writing data to the Internet resource.
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        Stream EndGetRequestStream(IAsyncResult asyncResult);
        WebResponse EndGetResponse(IAsyncResult asyncResult);

        void Abort();
    }
}
