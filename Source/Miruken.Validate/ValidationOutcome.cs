﻿namespace Miruken.Validate
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;

    public class ValidationOutcome : IDataErrorInfo, INotifyDataErrorInfo
    {
        private readonly ConcurrentDictionary<string, List<object>> _errors = new();
        private bool _initialized;
        private object _lock;

        private string _errorDetails;

        public bool     IsValid   => !HasErrors;
        public bool     HasErrors => !_errors.IsEmpty;
        public string   Error     => GetErrorDetails();
        public string[] Culprits  => _errors.Keys.ToArray();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public string this[string propertyName]
        {
            get
            {
                var outcome = ParsePath(ref propertyName);
                if (outcome == null) return string.Empty;
                if (outcome != this) return outcome[propertyName];
                if (!_errors.TryGetValue(propertyName, out var propertyErrors))
                    return string.Empty;
                return string.Join(Environment.NewLine, propertyErrors
                    .Select(err => err is ValidationOutcome
                        ? $"[{Environment.NewLine}{err}{Environment.NewLine}]" 
                        : err).ToArray());
            }
        }

        public IEnumerable GetErrors(string propertyName)
        {
            var outcome = ParsePath(ref propertyName);
            if (outcome == null) return Array.Empty<object>();
            if (outcome != this) return outcome.GetErrors(propertyName);
            return _errors.TryGetValue(propertyName, out var errors)
                 ? errors.AsReadOnly() : Enumerable.Empty<object>();
        }

        public ValidationOutcome GetOutcome(string propertyName) => GetOrCreateOutcome(propertyName);

        public ValidationOutcome AddError(string propertyName, object error)
        {
            var outcome = ParsePath(ref propertyName, true);
            _initialized = false;
            if (outcome == this)
            {
                _errors.GetOrAdd(propertyName, _ => new List<object>()).Add(error);
                RaiseErrorsChanged(propertyName);
            }
            else
                outcome.AddError(propertyName, error);
            return this;
        }

        private ValidationOutcome GetOrCreateOutcome(string propertyName, bool create = false)
        {
            var errors = _errors.GetOrAdd(propertyName, _ => new List<object>());
            lock (errors)
            {
                var outcome = errors.OfType<ValidationOutcome>().FirstOrDefault();
                if (outcome != null || !create) return outcome;
                outcome = new ValidationOutcome();
                errors.Add(outcome);
                outcome.ErrorsChanged += (_, e) =>
                    RaiseErrorsChanged($"{propertyName}.{e.PropertyName}");
                return outcome;
            }
        }

        private void RaiseErrorsChanged(string propertyName) =>
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        private ValidationOutcome ParsePath(ref string propertyName, bool create = false)
        {
            var outcome = this;
            while (outcome != null)
            {
                var index = ParseIndexer(ref propertyName);
                if (index != null)
                {
                    if (string.IsNullOrEmpty(propertyName))
                    {
                        propertyName = index;
                        return outcome;
                    }
                    outcome = outcome.GetOrCreateOutcome(index, create);
                }
                else
                {
                    var dot  = propertyName.IndexOf('.');
                    var open = propertyName.IndexOf('[');
                    if (dot > 0 || open > 0)
                    {
                        string rest;
                        if (dot > 0 && (open < 0 || dot < open))
                        {
                            rest = propertyName[(dot + 1)..];
                            propertyName = propertyName[..dot];
                        }
                        else
                        {
                            rest = propertyName[open..];
                            propertyName = propertyName[..open];
                        }
                        if (string.IsNullOrEmpty(rest)) return outcome;
                        outcome = outcome.GetOrCreateOutcome(propertyName, create);
                        propertyName = rest;
                    }
                    else
                        return outcome;
                }
            }
            return null;
        }

        private static string ParseIndexer(ref string propertyName)
        {
            var start = propertyName.IndexOf('[');
            if (start != 0) return null;
            var end = propertyName.IndexOf(']', 1);
            if (end <= start)
                throw new ArgumentException("Invalid property indexer.");
            var index = propertyName.Substring(1, end - 1);
            if (string.IsNullOrEmpty(index))
                throw new ArgumentException("Missing property index.");
            propertyName = propertyName[(end + 1)..].Trim('.');
            return index;
        }

        private string GetErrorDetails()
        {
            return LazyInitializer.EnsureInitialized(
                ref _errorDetails, ref _initialized, ref _lock,
                () => string.Join(Environment.NewLine, _errors.SelectMany(e =>
                {
                    var property = e.Key;
                    return e.Value.Select(err => err is ValidationOutcome
                         ? $"{property}[{Environment.NewLine}{err}{Environment.NewLine}]"
                         : err);
                }).ToArray()));
        }

        public override string ToString() => GetErrorDetails();
    }
}
