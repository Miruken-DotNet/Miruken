namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;
    using Infrastructure;

    public delegate bool ReturnTestDelegate(
        Type returnType, ParameterInfo[] parameters,
        RuleContext context);

    public class Return : ReturnRule
    {
        private readonly Type _type;

        public Return(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            _type = type;
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            CategoryAttribute category,
            RuleContext context)
        {
            return returnType.IsClassOf(_type);
        }

        public static readonly Return Void = new Return(typeof(void));

        public static Return Type(Type type)
        {
            return new Return(type);
        }

        public static TestReturn Is(ReturnTestDelegate test)
        {
            return new TestReturn(test);
        }
    }

    public class Return<T> : Return
    {
        public static readonly ReturnRule Instance = new Return<T>();

        public new static readonly ReturnRule OrVoid = Instance.OrVoid;

        private Return() : base(typeof(T))
        {         
        }
    }

    public class TestReturn : ReturnRule
    {
        private readonly ReturnTestDelegate _test;

        public TestReturn(ReturnTestDelegate test)
        {
            if (test == null)
                throw new ArgumentNullException(nameof(test));
            _test = test;
        }

        public override bool Matches(
             Type returnType, ParameterInfo[] parameters,
             CategoryAttribute category,
             RuleContext context)
        {
            return _test(returnType, parameters, context);
        }
    }
}
