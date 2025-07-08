using FluentSignals.Http.Contracts;
using FluentSignals.Http.Core;
using FluentSignals.Http.Factories;
using Playground.Console.Middleware;
using Playground.Console.Models;
using System;

namespace Playground.Console.Services;

public class UserHttpService(HttpResourceFactory httpResourceFactory)
{
    public HttpResource<List<User>> GetUsers()
    {
        return httpResourceFactory.Create<List<User>>("/api/users");
    }
}

