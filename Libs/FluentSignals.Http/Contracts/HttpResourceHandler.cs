using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentSignals.Http.Contracts;

public delegate Task<HttpResponseMessage> HttpResourceHandler(HttpRequestMessage request, CancellationToken cancellationToken);
