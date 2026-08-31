# mcp-router Refactoring Tools

Reference list of refactoring tools available via mcp-router (RefactorMCP-style Roslyn tools). Exact schemas are provided by the MCP server at runtime.

| Tool | Purpose |
|------|---------|
| `convert-to-constructor-injection` | Convert property/service locator usage to constructor injection |
| `convert-to-extension-method` | Convert a static helper into an extension method |
| `create-adapter` | Wrap an external type behind an adapter |
| `extract-decorator` | Extract a decorator around a type |
| `extract-interface` | Extract an interface from a class |
| `extract-method` | Extract selected statements into a named method |
| `feature-flag-refactor` | Wrap behavior behind a feature flag |
| `inline-method` | Inline a single-use method body |
| `introduce-field` | Promote an expression/constant to a named field/const |
| `introduce-parameter` | Introduce a parameter replacing an internal lookup |
| `introduce-variable` | Extract an expression into a local variable |
| `make-field-readonly` | Enforce immutability on a field |
| `make-static-then-move` | Make an instance member static to enable moving it |
| `move-instance-method` | Move an instance method to another class |
| `move-multiple-methods-instance` | Move several instance methods at once |
| `move-multiple-methods-static` | Move several static methods at once |
| `move-static-method` | Move a static method to another class |
| `move-to-separate-file` | Move a type into its own file |
| `rename-symbol` | Rename any symbol with reference updates |
| `safe-delete-field` | Delete a field with safety checks |
| `safe-delete-method` | Delete a method with safety checks |
| `safe-delete-parameter` | Delete an unused parameter with safety checks |
| `safe-delete-variable` | Delete an unused variable with safety checks |
| `transform-setter-to-init` | Convert a setter to `init` |
| `use-interface` | Depend on an extracted interface instead of the concrete type |

Named refactorings from the plan that map to these tools are recorded in `Docs/_Current/refactoring-plan.md` (steps R14–R18, all applied).
