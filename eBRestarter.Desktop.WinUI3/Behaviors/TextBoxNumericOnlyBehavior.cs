using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using System.Text.RegularExpressions;

namespace eBRestarter.Desktop.WinUI3.Behaviors;

public class TextBoxNumericOnlyBehavior : Behavior<TextBox>
{
    #region ConstantsAndStatics

    private static readonly Regex _regex = new("[^0-9]+");

    #endregion

    #region ProtectedMethods

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

    #endregion

    #region PrivateHelperMethods

    private void OnTextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        if (_regex.IsMatch(sender.Text))
        {
            int pos = sender.SelectionStart;
            sender.Text = _regex.Replace(sender.Text, "");
            sender.SelectionStart = System.Math.Min(pos, sender.Text.Length);
        }
    }

    #endregion
}
