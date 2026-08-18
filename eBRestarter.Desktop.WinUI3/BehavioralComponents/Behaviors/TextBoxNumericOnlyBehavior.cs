using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Behaviors;

public sealed class TextBoxNumericOnlyBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.TextChanging += OnTextChanging;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.TextChanging -= OnTextChanging;
    }

    private static void OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        string currentText = sender.Text;

        if (string.IsNullOrEmpty(currentText))
        {
            return;
        }

        ReadOnlySpan<char> span = currentText.AsSpan();

        if (!HasNonDigits(span))
        {
            return;
        }

        int originalSelectionStart = sender.SelectionStart;

        string cleanText = RemoveNonDigits(span);

        sender.Text = cleanText;
        sender.SelectionStart = Math.Min(originalSelectionStart, cleanText.Length);
    }

    private static bool HasNonDigits(ReadOnlySpan<char> span)
    {
        foreach (char c in span)
        {
            if (!char.IsAsciiDigit(c))
            {
                return true;
            }
        }

        return false;
    }

    private static string RemoveNonDigits(ReadOnlySpan<char> source)
    {
        int digitCount = 0;
        foreach (char c in source)
        {
            if (char.IsAsciiDigit(c))
            {
                digitCount++;
            }
        }

        if (digitCount == 0)
        {
            return string.Empty;
        }

        return string.Create(digitCount, source, static (destination, span) =>
        {
            int index = 0;
            foreach (char c in span)
            {
                if (char.IsAsciiDigit(c))
                {
                    destination[index++] = c;
                }
            }
        });
    }
}