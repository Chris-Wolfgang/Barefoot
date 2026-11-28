type: feature

`bf-maint assign-owner <oid>` moves the searches, recipients, open issues and digest deliveries of the seeded "Unassigned" user to a user who has signed in. It refuses, and moves nothing, when that user already has a search of the same name or a recipient of the same address.
