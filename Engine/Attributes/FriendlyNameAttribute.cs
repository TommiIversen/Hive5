namespace Engine.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FriendlyNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}