# ActorAbilitySystem
## Table of Contents
[Intro](#Intro)
[Install](#Install)

## Intro 
This unity package provides AbilitySystem for managing actor's ability and states.

In this context, actor is meaning an object that exists in the world.

You can use these features
 - Add and remove ability to actor.
 - Store and calculate actor's attributes.
 - Change attribute or state with effect.

## Install
You need to add ScopedRegistry to `manifest.json`
```
"scopedRegistries": [
  {
    "name": "QuestionPackages", // You can change it to any name you want.
    "url": "https://registry.npmjs.org/",
    "scopes": [
      "com.question"
    ],
    "overrideBuiltIns": false
  }
]
```

Then you can add `https://github.com/NamgyeongHeo/ActorAbiltySystem.git?path=Assets/Plugins/ActorAbilitySystem` to UPM by git url.
