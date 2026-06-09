using System.Collections.Generic;
using Luminix.Primitives;

namespace Luminix.Threading.Buffers;

internal class MessageBuffer
{
    private Dictionary<string, IValue> messages = new Dictionary<string, IValue>();

    public void PushMessage(string valueName, IValue value)
    {
        if(messages.TryGetValue(valueName, out IValue existingValue))
        {
            var existingType = existingValue.GetType();
            var incomingType = value.GetType();
            if (incomingType != existingType)
            {
                Logger.Get().Wrn($"Type mismatch for message '{valueName}'. Cannot overwrite existing value of type '{existingType.Name}' with a value of type '{incomingType.Name}'.");
                return;
            }
        }

        messages[valueName] = value;
    }

    public IValue GetMessage(string valueName)
    {
        if(messages.TryGetValue(valueName, out IValue value))
            return value;
        
        return null;
    }
}