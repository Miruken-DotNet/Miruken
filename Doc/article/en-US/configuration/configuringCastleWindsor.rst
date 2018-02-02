==========================
Configuring Castle Windsor
==========================

.. note:: Miruken has first class integration with Castle Windsor, but Miruken does not require you to use Castle Windsor for your container. Miruken does not even require you to use a container.  All of that being said, we love Castle Windsor and use it in our own projects.

One of the main ways of configuring a Castle Windsor container is the :code:`Container.Install()` method.  It accepts a comma seperated list of :code:`IWindsorInstaller` instances.  These installers do all the work of registering objects and configuring the container.

In this very basic Castle Windsor Container all the :code:`IWindsorInstaller` classes in the current assembly will be run. :code:`FromAssembly.This()` returns an :code:`IWindsorInstaller`.

.. literalinclude:: ../../../examples/mirukenCastleExamples/basicWindsorContainer.cs

We used to use this simple form of configuration, but found that we had to list assemblies multiple times. Features and FeatureInstaller solve this problem. 

Features
========

At a high level a feature is an implementation of a Miruken concept. It may be a Protocol, Handler, Validator, or Mediator, etc.  On a very practical level features are concrete application code implemented across multiple assemblies. The :code:`Features` object has several ways to specify your application assemblies so that they can be installed in the container.  Using Features allows you to specify all your assemblies in one place.

FeatureInstaller
================

FeatureInstallers inherit from :code:`FeatureInstaller` and do the container registration and configuration for a Miruken concept across all your feature assemblies.

FromAssemblies(params Assembly[] assemblies)
--------------------------------------------

In this example we pass a comma seperated list of application assemblies into::

   Features.FromAssemblies()
   
==============================  ====================================
typeof(CreateTeam).Assembly     Targets the Example.League assembly.
typeof(CreateStudent).Assembly  Targets the Example.School assembly.
==============================  ====================================

Next, we specify which FeatureInstallers the application needs.  This example configures the ConfigurationFactory using the ConfigurationFactoryInstaller, and Validation using the ValidationInstaller.

.. literalinclude:: ../../../examples/mirukenCastleExamples/featuresFromAssemblies.cs
   :emphasize-lines: 21-25

FromAssembliesNamed(params string[] assemblyNames)
--------------------------------------------------

The FromAssembliesNamed() method allows you to specify the assembly name of the feature assemblies you want installed into the container.

.. literalinclude:: ../../../examples/mirukenCastleExamples/featuresFromAssembliesNamed.cs
   :emphasize-lines: 19-21

InDirectory(AssemblyFilter filter)
----------------------------------

The InDirectory() method allows you to specify an AssemblyFilter. An AssemblyFilter takes the string name of a directory and a filter predicate to allow only the assemblies you intend.

.. literalinclude:: ../../../examples/mirukenCastleExamples/featuresInDirectory.cs
   :emphasize-lines: 20-21



