plugins {
    alias(libs.plugins.androidApplication)
}

android {
    namespace = "org.psr.gsr"
    compileSdk = 34

    defaultConfig {
        applicationId = "org.psr.gsr"
        minSdk = 21
        targetSdk = 34

        versionName = getGitVersion()
        versionCode = getBuildVersionCode()
    }

    signingConfigs {
        create("release") {
            if (project.hasProperty("keystore")) {
                storeFile = file(project.property("keystore")!!)
                storePassword = project.property("storepass").toString()
                keyAlias = project.property("keyalias").toString()
                keyPassword = project.property("keypass").toString()
            }
        }
    }

    buildTypes {
        release {
            if (project.hasProperty("keystore")) {
                signingConfig = signingConfigs.getByName("release")
                if (getIsNightlyVersion()) {
                    resValue("string", "app_name", "GSR Nightly")
                    applicationIdSuffix = ".nightly"
                    versionNameSuffix = "-nightly"
                }
            } else {
                // this path would be taken by PRs
                resValue("string", "app_name", "GSR Canary")
                applicationIdSuffix = ".canary"
                versionNameSuffix = "-canary"
                signingConfig = signingConfigs.getByName("debug")
            }

            isMinifyEnabled = false
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
        }

        debug {
           resValue("string", "app_name", "GSR Debug")
           applicationIdSuffix = ".debug"
           versionNameSuffix = "-debug"
           signingConfig = signingConfigs.getByName("debug")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    sourceSets.getByName("main") {
        jniLibs.srcDirs("src/main/libs")
    }
}

// Some git functions copied over from Dolphin (with some modifications)

// Usually matches what we do with git info (although omits dirty/branch inclusion)
fun getGitVersion(): String {
    try {
        return ProcessBuilder("git", "describe", "--always", "--long")
            .directory(project.rootDir)
            .redirectOutput(ProcessBuilder.Redirect.PIPE)
            .redirectError(ProcessBuilder.Redirect.PIPE)
            .start().inputStream.bufferedReader().use { it.readText() }
            .trim()
            .replace(Regex("(-0)?-[^-]+$"), "")
            .replace("-", ".")
    } catch (e: Exception) {
        logger.error("Cannot find git, defaulting to dummy version number")
    }

    return "0.0"
}

// Hidden version code, used internally for knowing what version is "newer", needs to monotonically increment
fun getBuildVersionCode(): Int {
    try {
        return Integer.valueOf(
            ProcessBuilder("git", "rev-list", "--count", "HEAD")
                .directory(project.rootDir)
                .redirectOutput(ProcessBuilder.Redirect.PIPE)
                .redirectError(ProcessBuilder.Redirect.PIPE)
                .start().inputStream.bufferedReader().use { it.readText() }
                .trim()
        )
    } catch (e: Exception) {
        logger.error("Cannot find git, defaulting to dummy version code")
    }

    return 1
}

// Kind of a bad check, naively assumes any tag is a release
fun getIsNightlyVersion(): Boolean {
    try {
        return ProcessBuilder("git", "name-rev", "--name-only", "--tags", "HEAD")
            .directory(project.rootDir)
            .redirectOutput(ProcessBuilder.Redirect.PIPE)
            .redirectError(ProcessBuilder.Redirect.PIPE)
            .start().inputStream.bufferedReader().use { it.readText() }
            .trim() == "undefined"
    } catch (e: Exception) {
        logger.error("Cannot find git, defaulting to nightly version")
    }

    return true
}
