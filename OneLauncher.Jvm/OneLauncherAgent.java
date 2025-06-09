import net.bytebuddy.agent.builder.AgentBuilder;
import net.bytebuddy.asm.Advice;
import net.bytebuddy.matcher.ElementMatchers;

import java.lang.instrument.Instrumentation;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;

import static net.bytebuddy.matcher.ElementMatchers.named;
import static net.bytebuddy.matcher.ElementMatchers.takesArguments;

public class OneLauncherAgent {

    private static final String TITLE_PROPERTY_KEY = "one.launcher.custom.title";

    public static void premain(String agentArgs, Instrumentation inst) {
        if (agentArgs == null || agentArgs.isEmpty()) {
            System.err.println("[OneLauncherAgent] Agent arguments are missing!");
            return;
        }

        // --- 这是新增的核心逻辑 ---
        String[] parts = agentArgs.split(";", 2);
        if (parts.length < 2) {
            System.err.println("[OneLauncherAgent] Invalid agent arguments format. Expected 'title;byteBuddyPath'");
            return;
        }

        String title = parts[0];
        String byteBuddyJarPath = parts[1];

        try {
            // 手动将 byte-buddy.jar 添加到系统类加载器的搜索路径中
            inst.appendToSystemClassLoaderSearch(new JarFile(new File(byteBuddyJarPath)));
            System.out.println("[OneLauncherAgent] Successfully injected Byte Buddy into system classloader.");
        } catch (Exception e) {
            System.err.println("[OneLauncherAgent] Failed to inject Byte Buddy jar: " + byteBuddyJarPath);
            e.printStackTrace();
            return;
        }
        // --- 核心逻辑结束 ---

        System.setProperty(TITLE_PROPERTY_KEY, title);
        System.out.println("[OneLauncherAgent] Custom title received: " + title);

        new AgentBuilder.Default()
            .with(new AgentBuilder.Listener.WithErrorsOnly(new AgentBuilder.Listener.StreamWriting(System.err)))
            .ignore(ElementMatchers.none()) // 加上这行，让它可以匹配所有类加载器
            .type(ElementMatchers.named("org.lwjgl.glfw.GLFW"))
            .transform((builder, type, cl, m, pd) -> builder
                .method(named("glfwSetWindowTitle").and(takesArguments(long.class, CharSequence.class)))
                .intercept(Advice.to(Lwjgl3CharSequenceAdvice.class))
                .method(named("glfwSetWindowTitle").and(takesArguments(long.class, ByteBuffer.class)))
                .intercept(Advice.to(Lwjgl3ByteBufferAdvice.class))
            )
            .type(ElementMatchers.named("org.lwjgl.opengl.Display"))
            .transform((builder, type, cl, m, pd) -> builder
                .method(named("setTitle").and(takesArguments(String.class)))
                .intercept(Advice.to(Lwjgl2Advice.class))
            )
            .installOn(inst);
    }
    // LWJGL 3 - CharSequence
    public static class Lwjgl3CharSequenceAdvice {
        @Advice.OnMethodEnter
        public static void onEnter(@Advice.Argument(value = 1, readOnly = false) CharSequence title) {
            String newTitle = System.getProperty(TITLE_PROPERTY_KEY);
            if (newTitle != null) title = newTitle;
        }
    }

    // LWJGL 3 - ByteBuffer
    public static class Lwjgl3ByteBufferAdvice {
        @Advice.OnMethodEnter
        public static void onEnter(@Advice.Argument(value = 1, readOnly = false) ByteBuffer title) {
            String newTitle = System.getProperty(TITLE_PROPERTY_KEY);
            if (newTitle != null) {
                byte[] bytes = newTitle.getBytes(StandardCharsets.UTF_8);
                ByteBuffer buffer = ByteBuffer.allocateDirect(bytes.length + 1);
                buffer.put(bytes).put((byte) 0).flip();
                title = buffer;
            }
        }
    }

    // LWJGL 2 - String
    public static class Lwjgl2Advice {
        @Advice.OnMethodEnter
        public static void onEnter(@Advice.Argument(value = 0, readOnly = false) String title) {
            // 注意 LWJGL 2 的 setTitle 只有一个参数，所以是 @Advice.Argument(0)
            String newTitle = System.getProperty(TITLE_PROPERTY_KEY);
            if (newTitle != null) {
                title = newTitle;
            }
        }
    }
}