namespace FluentSignals.WebAssembly.Models;

public class PersonForAddDto
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public PersonForAddDto() { }
    
    public PersonForAddDto(string name, int age)
    {
        Name = name;
        Age = age;
    }
}