# Mini-Git

A git implementation written in c#.

Currently it doesn't have 'staging area' so all files in the current working directory are staged.

## Supported commands
| Command | Description |
|---|---|
| init | Initialise a new repository |
| commit | Commit current directory to main |
| log | Show commit logs |
| diff | Show changes between commits |

## Plumbing commands
| Command | Description |
|---|---|
| cat-file | Display contents or details of repository objects |
| hash-object | Compute object hash and optionally create an object from a file |
| ls-tree | List the contents of a tree object |
| write-tree | Create a tree object from the current directory |