using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alter.Lib;

namespace SyntaxAlter.components;

public partial class JqueryDependencyRemover : ContentView
{
    public JqueryDependencyRemover()
    {
        InitializeComponent();
    }
    private void OnClearJqueryClicked(object? sender, EventArgs e)
    {
        string cleared = JQueryDependencyRemover.RemoveJQueryDependencies(JsBox.Text);
        NoJquery.Text = cleared;
    }
}