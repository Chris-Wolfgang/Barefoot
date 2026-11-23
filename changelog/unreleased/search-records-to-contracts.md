type: internal

Move `SearchDefinition` and the `Brand`, `Cabin` and `WindowKind` enums from `Core` to `Contracts`: they are what a client sends, and a record's enums live with it (ADR 0002 §4).
