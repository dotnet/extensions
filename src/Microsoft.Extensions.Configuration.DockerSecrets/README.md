
This is a configuration provider forthe up-coming secrets feature of Docker 1.13.

You can use this provider in an ASP.NET Core application that is running in Docker Swarm Mode with commands like the following:

```
$ echo "mySecretValue" | docker secrets create mysecret

```

```
$ docker service create --name test --secret mysecret <myImageName>
```

Now the `test` service will be running and have access to a secret called mysecret. In your app you can access it with

```
var secretValue = Configuration["mysecret"];
```
