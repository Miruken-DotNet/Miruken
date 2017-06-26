============================
Castle Windsor Configuration
============================

:note: Miruken does not require you to use Castle Windsor. Miruken does not even require you to use a container, but we really like Castle Windsor!

One of the main ways of configuring Castle Windsor is the :code:`Container.Install()` method.  It accepts a comma seperated list of :code:`IWindsorInstaller` instances.  These installers do all the work of registering objects and configuring the container.

In this very basic Castle Windsor Container all the :code:`IWindsorInstaller` classes in the current assembly will be run. :code:`FromAssembly.This()` returns an :code:`IWindsorInstaller`.

.. literalinclude:: /example/mirukenCastleExamples/basicWindsorContainer.cs

Features.FromAssemblies(params Assembly[] assemblies)
=====================================================

Miruken has first class integration with Castle Windsor. 
The :code:`Features` object has several ways to specify your application assemblies. In this example we pass a comma seperated list of application assemblies into :code:`Features.FromAssemblies()`.
:code:`typeof(CreateTeam).Assembly` targets the Example.League assembly and 
:code:`typeof(CreateStudent).Assembly` targets the Example.School assembly. 
Using Features allows you to only specify your application assemblies once, and it guaranties that your assemblies are only scanned once for Miruken code.

Next, you specify which Miruken installers you want to run. These installers inherit from 
:code:`FeatureInstaller`, and do all the work of registering objects and configuring the container for that specific feature across all your application assemblies.  This example configures the ConfigurationFactory using the ConfigurationFactoryInstaller, and Validation using the ValidationInstaller.

.. literalinclude:: /example/mirukenCastleExamples/featuresFromAssembly.cs

