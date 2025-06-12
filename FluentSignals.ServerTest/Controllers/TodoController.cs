using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace FluentSignals.ServerTest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoController : ControllerBase
{
    private static readonly ConcurrentDictionary<int, TodoItem> _todos = new();
    private static int _nextId = 1;

    static TodoController()
    {
        // Add some initial data
        AddTodo(new TodoItem { Title = "Learn FluentSignals", Completed = true });
        AddTodo(new TodoItem { Title = "Build awesome reactive UI", Completed = false });
        AddTodo(new TodoItem { Title = "Share with the community", Completed = false });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? delay = null)
    {
        if (delay.HasValue)
        {
            await Task.Delay(delay.Value);
        }

        return Ok(_todos.Values.OrderBy(t => t.Id));
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        if (_todos.TryGetValue(id, out var todo))
        {
            return Ok(todo);
        }
        return NotFound(new { message = "Todo not found" });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TodoItem todo)
    {
        await Task.Delay(500); // Simulate processing

        if (string.IsNullOrWhiteSpace(todo.Title))
        {
            return BadRequest(new { message = "Title is required" });
        }

        var newTodo = AddTodo(todo);
        return CreatedAtAction(nameof(GetById), new { id = newTodo.Id }, newTodo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TodoItem todo)
    {
        await Task.Delay(300); // Simulate processing

        if (!_todos.ContainsKey(id))
        {
            return NotFound(new { message = "Todo not found" });
        }

        todo.Id = id;
        _todos[id] = todo;
        return Ok(todo);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await Task.Delay(300); // Simulate processing

        if (_todos.TryRemove(id, out var removed))
        {
            return Ok(removed);
        }
        return NotFound(new { message = "Todo not found" });
    }

    [HttpPost("{id}/toggle")]
    public IActionResult Toggle(int id)
    {
        if (_todos.TryGetValue(id, out var todo))
        {
            todo.Completed = !todo.Completed;
            return Ok(todo);
        }
        return NotFound(new { message = "Todo not found" });
    }

    private static TodoItem AddTodo(TodoItem todo)
    {
        todo.Id = _nextId++;
        todo.CreatedAt = DateTime.UtcNow;
        _todos[todo.Id] = todo;
        return todo;
    }
}

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool Completed { get; set; }
    public DateTime CreatedAt { get; set; }
}