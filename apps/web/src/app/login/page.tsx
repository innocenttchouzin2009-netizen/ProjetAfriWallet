import LoginForm from '@/features/auth/components/LoginForm';

export default function LoginPage() {
  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
      <div className="mx-auto max-w-2xl">
        <LoginForm />
      </div>
    </main>
  );
}
