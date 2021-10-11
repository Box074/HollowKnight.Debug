

```json
{
  "colors": [
    {
      "layer": "PLAYER",
      "needComponents": [],
      "needPlayMakerFSMs": [],
      "r": 0.0,
      "g": 1.0,
      "b": 0.0,
      "includeDisable": false
    }
  ]
}
```



`layer`(类型`GlobalEnums.PhysLayers`): 当不为`DEFAULT`时，要求目标`GameObject`的`layer`的值与其相同 (默认值：`DEFAULT`)

`needComponents`(类型`List<string>`): 当不为空时，要求目标`GameObject`需要拥有所有指定的组件

`needPlayMakerFSMs`(类型`List<string>`): 当不为空时，要求目标`GameObject`需要拥有所有指定名称的`PlayMakerFSM`

`r`,`g`,`b`(类型`float`): 线条颜色

`includeDisable`(类型`bool`): 未使用