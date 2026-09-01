# Refactoring tools (mcp-router server)

Observed at runtime: this sandbox does NOT have the RefactorMCP server connected (only WorkflowsMcp-http is available), so refactoring tool calls (extract-method, rename-symbol, safe-delete-*, move-*, introduce-*, inline-method, convert-to-*, make-static-then-move, use-interface, feature-flag-refactor) are NOT callable here.

Per plan-refactoring step, the tooling map in Docs/_Current/refactoring-plan.md records the intended tool per step; where the server is absent, the same standard named refactorings (Rider/ReSharper "Main Set": Extract Method, Rename, Introduce Constant, Encapsulate Collection, Move Method, Replace State with Properties, Inline Method, Safe Delete) were applied by manual edits with equivalent semantics, each gated by build + full test suite.

Tool reference (for environments where RefactorMCP is connected): see GitHub dave-hillier/refactor-mcp and its EXAMPLES.md; common params: solutionPath, filePath + line/column or selectionRange, className/methodName/propertyName/fieldName, oldName/newName/targetClass/targetFilePath.
