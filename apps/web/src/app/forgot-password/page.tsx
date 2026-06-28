import ForgotPasswordForm from '@/features/auth/components/ForgotPasswordForm';

export default function ForgotPasswordPage() {
  return (
    <main className="min-h-screen bg-[#0D0D0D] px-6 py-28 text-white md:px-16">
      <div className="mx-auto max-w-2xl">
        <ForgotPasswordForm />
      </div>
    </main>
  );
}
