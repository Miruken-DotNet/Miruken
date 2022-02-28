namespace Miruken.Validate;

using System;
using System.Collections;
using System.ComponentModel;

public class ValidationAware 
    : IValidationAware, IDataErrorInfo, INotifyDataErrorInfo
{
    private ValidationOutcome _outcome;

    public string Error                     => ValidationOutcome?.Error;
    public bool   HasErrors                 => ValidationOutcome?.HasErrors == true;
    public string this[string propertyName] => ValidationOutcome?[propertyName];

    public ValidationOutcome ValidationOutcome
    {
        get => _outcome;
        set
        {
            if (ReferenceEquals(_outcome, value)) return;
            if (_outcome != null)
                _outcome.ErrorsChanged -= DataErrorsChanged;
            _outcome = value;
            if (_outcome != null)
                _outcome.ErrorsChanged += DataErrorsChanged;
        }
    }

    public IEnumerable GetErrors(string propertyName) =>
        ValidationOutcome?.GetErrors(propertyName);

    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    private void DataErrorsChanged(object sender, DataErrorsChangedEventArgs e)
    {
        ErrorsChanged?.Invoke(this, e);
    }
}