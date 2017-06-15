=======
Promise
=======

We didn't set out to write a promise library in C#.  It was born out of necessity.
We inherited a C# project written in CE 6 which had no async and await support.
We found that doing asynchronous work by hand is verbose and error prone so we
needed a better way.  Thus Miruken's implementation of the
`A+ Promise Specification <https://promisesaplus.com/>`_ was born.
Surprisingly we really like using promises in C#. Especially, since we often
switch back and forth between JavaScript and C#.  The code looks and feels
very simular.
