namespace Miruken.Callback.Policy.Rules
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
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters,
            RuleContext context)
        {
            return returnType.IsClassOf(_type);
        }

        public static readonly Return Void = new Return(typeof(void));

        public static Return Of(Type type)
        {
            return new Return(type);
        }

        public static Return Of<T>()
        {
            return Of(typeof(T));
        }

        public new static ReturnRule Alias(string alias)
        {
            return TestReturn.True.Alias(alias);
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

        public static readonly TestReturn True = Return.Is((_, _, _) => true);

        public TestReturn(ReturnTestDelegate test)
        {
            _test = test ?? throw new ArgumentNullException(nameof(test));
        }

        public override bool Matches(
             Type returnType, ParameterInfo[] parameters,
             RuleContext context)
        {
            return _test(returnType, parameters, context);
        }
    }
}
