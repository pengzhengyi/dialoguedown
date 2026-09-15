<!-- broken: two arms are given the same order

     "out": [
       { "kind": "branch", "target": 1, "order": 0, "condition": { "kind": "key", "key": "Alice.HasMap" } },
       { "kind": "branch", "target": 2, "order": 0, "condition": { "kind": "key", "key": "Alice.HasLantern" } },
       { "kind": "branch", "target": 3, "order": 1 }
     ]
-->
> `if` `Alice.HasMap?`
>
> Alice: You have the map.
>
> `elseif` `Alice.HasLantern?`
>
> Alice: You have the lantern.
>
> `else`
>
> Alice: You have neither.
