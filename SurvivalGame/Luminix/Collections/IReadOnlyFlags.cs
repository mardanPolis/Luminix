
namespace Luminix.Collections;

public interface IReadOnlyFlags
{
    bool this[string flag] { get; }
    bool HasFlag(string flag);
}