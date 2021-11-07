xcopy AllNetTest\Build\WebGL\*.* allegg.github.io\ /e /h /k /s /Y
cd allegg.github.io
git add *
git commit -m "deploy"
git push
cd ..