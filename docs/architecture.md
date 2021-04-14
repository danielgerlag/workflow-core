# Workflow Core Architecture

**Host** manages the workflows. Workflows are registered and started here.
The host also provides error and lifecycle events.

**Registry** contains the workflow definitions with their steps

**Persistence** persists the workflow states with its execution pointers

**Execution Pointer** is created when a step is visited during a workflow.
Fields like its status or end date indicate if and how the step was completed.

**Workflow Definition** is the description of a workflow with steps.
The workflow definition provides an ID. When a workflow is started,
every workflow *instance* obtains a unique ID (but still holds the ID
of the workflow definition).
