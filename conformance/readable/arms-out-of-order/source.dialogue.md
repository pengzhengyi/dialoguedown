<!-- broken: the two gated arms are listed in descending order, 1 before 0

     "out": [
       { "kind": "branch", "target": 1, "order": 1, "condition": { "kind": "key", "key": "Alice.HasMap" } },
       { "kind": "branch", "target": 2, "order": 0, "condition": { "kind": "key", "key": "Alice.HasLantern" } },
       { "kind": "branch", "target": 3, "order": 2 }
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
