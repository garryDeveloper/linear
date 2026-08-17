# Domain Model

## Overview

Este proyecto es un clon simplificado de Linear enfocado en gestión de proyectos, issues y planificación de trabajo.

Las entidades principales son:

* User
* Team
* TeamMember
* Issue
* Comment
* Label
* Sprint
* Roadmap
* RoadmapItem
* Activity

---

# User

Representa una persona que utiliza el sistema.

## Fields

```text
Id
Email
Name
AvatarUrl
IsActive
CreatedAt
UpdatedAt
```

## Relationships

```text
User
 ├── TeamMemberships
 ├── AssignedIssues
 ├── CreatedIssues
 ├── Comments
 └── Activities
```

---

# Team

Unidad organizativa principal.

Todos los issues pertenecen a un equipo.

## Fields

```text
Id
Name
Key
Description
CreatedAt
UpdatedAt
```

## Rules

* El nombre es obligatorio.
* La key debe ser única.
* Todos los issues pertenecen a un Team.

## Relationships

```text
Team
 ├── Members
 ├── Issues
 ├── Labels
 ├── Sprints
 ├── RoadmapItems
 └── Activities
```

---

# TeamMember

Relaciona usuarios con equipos.

## Fields

```text
Id
TeamId
UserId
Role
JoinedAt
```

## Role Enum

```text
Owner
Admin
Member
```

## Rules

* Un usuario no puede pertenecer dos veces al mismo equipo.
* Todo equipo debe tener al menos un Owner.

---

# Issue

Entidad principal del sistema.

Representa una tarea, bug, mejora o historia.

## Fields

```text
Id
Identifier
Title
Description
Status
Priority
Estimate
TeamId
AssigneeId
CreatedById
SprintId
RoadmapItemId
CreatedAt
UpdatedAt
CompletedAt
ArchivedAt
```

## Status Enum

```text
Backlog
Todo
InProgress
InReview
Done
Canceled
```

## Priority Enum

```text
None
Low
Medium
High
Urgent
```

## Rules

* El título es obligatorio.
* Todo issue pertenece a un Team.
* Un issue puede no tener responsable.
* Un issue puede no pertenecer a un Sprint.
* Un issue puede no pertenecer a un RoadmapItem.

## Identifier

Formato:

```text
TEAM-1
TEAM-2
TEAM-3
```

Ejemplos:

```text
WEB-12
CORE-48
MOBILE-7
```

## Relationships

```text
Issue
 ├── Team
 ├── Assignee
 ├── CreatedBy
 ├── Sprint
 ├── Labels
 ├── Comments
 └── Activities
```

---

# Label

Categoriza issues.

## Fields

```text
Id
TeamId
Name
Description
Color
CreatedAt
```

## Rules

* El nombre es obligatorio.
* El nombre debe ser único dentro del Team.

## Relationships

```text
Label
 └── Issues
```

---

# IssueLabel

Tabla de relación muchos a muchos.

## Fields

```text
IssueId
LabelId
```

---

# Comment

Comentario dentro de un Issue.

Soporta Markdown.

## Fields

```text
Id
IssueId
AuthorId
Content
CreatedAt
UpdatedAt
DeletedAt
```

## Rules

* El contenido es obligatorio.
* El contenido se almacena como Markdown.

## Relationships

```text
Comment
 ├── Issue
 └── Author
```

---

# Sprint

Representa un período de trabajo.

## Fields

```text
Id
TeamId
Name
Goal
StartDate
EndDate
Status
CreatedAt
UpdatedAt
CompletedAt
```

## SprintStatus Enum

```text
Planned
Active
Completed
Canceled
```

## Rules

* Solo un Sprint activo por Team.
* EndDate debe ser mayor a StartDate.

## Relationships

```text
Sprint
 └── Issues
```

---

# Roadmap

Contenedor de iniciativas.

Permite agrupar trabajo a mediano y largo plazo.

## Fields

```text
Id
TeamId
Name
Description
CreatedAt
UpdatedAt
```

## Relationships

```text
Roadmap
 └── RoadmapItems
```

---

# RoadmapItem

Representa una iniciativa dentro del roadmap.

## Fields

```text
Id
RoadmapId
Name
Description
Status
StartDate
TargetDate
CreatedAt
UpdatedAt
```

## Status Enum

```text
Planned
InProgress
Completed
Canceled
```

## Relationships

```text
RoadmapItem
 ├── Issues
 └── Roadmap
```

---

# Activity

Registro histórico de cambios.

Permite construir Activity Feed, auditoría y tiempo real.

## Fields

```text
Id
TeamId
UserId

EntityType
EntityId

Action

PayloadJson

CreatedAt
```

## EntityType Enum

```text
Issue
Comment
Sprint
RoadmapItem
Label
Team
```

## Actions

```text
IssueCreated
IssueUpdated
IssueAssigned
IssueCompleted
IssueCanceled

CommentCreated
CommentUpdated

LabelAdded
LabelRemoved

SprintStarted
SprintCompleted

RoadmapItemCreated
RoadmapItemUpdated
```

## Payload Example

```json
{
  "oldValue": "Todo",
  "newValue": "InProgress"
}
```

## Rules

* Nunca se modifica.
* Nunca se elimina.
* Es append-only.

---

# Future Entities (Not in V1)

Estas entidades no deben implementarse todavía.

## Project

```text
Project
 └── Issues
```

## Cycle

Equivalente al concepto original de Linear.

```text
Cycle
 └── Issues
```

## Notification

```text
Notification
 └── User
```

## Attachment

```text
Attachment
 └── Issue
```

---

# Aggregate Boundaries

## Team Aggregate

```text
Team
 ├── TeamMembers
 ├── Labels
 └── Sprints
```

## Issue Aggregate

```text
Issue
 ├── Comments
 └── IssueLabels
```

## Roadmap Aggregate

```text
Roadmap
 └── RoadmapItems
```

---

# Initial Database Tables

```text
Users
Teams
TeamMembers

Issues
IssueLabels
Labels
Comments

Sprints

Roadmaps
RoadmapItems

Activities
```

---

# Initial Navigation Structure

```text
Home

Teams
 ├── Issues
 ├── Sprints
 ├── Roadmap
 └── Activity

Settings
```

## First MVP Flow

```text
Create Team
    ↓
Invite Members
    ↓
Create Labels
    ↓
Create Sprint
    ↓
Create Issues
    ↓
Assign Issues
    ↓
Comment Progress
    ↓
Track Activity
```
