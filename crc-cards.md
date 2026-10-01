## As-Is CRC Cards

No existing codebase. This is a greenfield project with no as-is classes.

## To-Be CRC Cards

### Program (Presentation Layer — new)
**State**: None (no fields or properties)

**Behavior**:
- Main entry point: calls Console.WriteLine("Hello, World!")

**Layer**: Presentation

**Collaborators**: None (uses System.Console statically, not held as a field or injected dependency)

**Validation**: Program is a Presentation layer class. It contains only a single Console.WriteLine call (pure I/O presentation logic). No domain logic, no data access, no business logic. Layer assignment is valid. No state, no calculations, no conditions or loops.
