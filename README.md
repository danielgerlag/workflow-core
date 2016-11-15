# Workflow Core

Workflow Core is a light weight workflow engine targeting .NET Standard 1.6.  It supports pluggable persistence and concurrency providers to allow for multi-node clusters.

## Getting Started


```C#
builder
    .StartWith<HelloWorld>()
    .Then<GoodbyeWorld>();
```



## Authors

* **Daniel Gerlag** - *Initial work*

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details


