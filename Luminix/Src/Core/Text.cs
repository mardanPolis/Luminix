using System;
using System.Text;

namespace Luminix;

public ref struct Text
{
    private readonly StringBuilder stringBuilder;
    private readonly ReadOnlySpan<char> text;

    public Text(ReadOnlySpan<char> text)
    {
        this.text = text;
        this.stringBuilder = null;
    }

    public Text(StringBuilder stringBuilder)
    {
        this.stringBuilder = stringBuilder;
        this.text = default;
    }
    public readonly int Length => stringBuilder != null ? stringBuilder.Length : text.Length;

    public char this[int index]
    {
        get
        {
            if (stringBuilder != null)
            {
                return stringBuilder[index];
            }
            return text[index];
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    public ref struct Enumerator
    {
        private readonly ReadOnlySpan<char> textSpan;
        private readonly StringBuilder stringBuilder;
        private int index;
        private readonly int length;
        private readonly bool isStringBuilder;

        public Enumerator(Text text)
        {
            textSpan = text.text;
            stringBuilder = text.stringBuilder;
            length = text.Length;
            index = -1;
            isStringBuilder = text.stringBuilder != null;
        }

        public bool MoveNext()
        {
            if (index < length - 1)
            {
                index++;
                return true;
            }
            return false;
        }

        public char Current
        {
            get
            {
                if (isStringBuilder)
                {
                    return stringBuilder![index];
                }
                return textSpan[index];
            }
        }
    }
}