import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile_app/services/flutter_secure_storage_adapter.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    FlutterSecureStorage.setMockInitialValues(<String, String>{});
  });

  test('writes and reads a value through native secure storage', () async {
    const adapter = FlutterSecureStorageAdapter();

    await adapter.write(key: 'afwal.auth.session.v1', value: 'session-json');

    expect(
      await adapter.read(key: 'afwal.auth.session.v1'),
      'session-json',
    );
  });

  test('returns null when the requested key does not exist', () async {
    const adapter = FlutterSecureStorageAdapter();

    expect(await adapter.read(key: 'missing-key'), isNull);
  });

  test('deletes only the requested secure-storage key', () async {
    FlutterSecureStorage.setMockInitialValues(<String, String>{
      'afwal.auth.session.v1': 'session-json',
      'unrelated-key': 'keep-me',
    });
    const adapter = FlutterSecureStorageAdapter();

    await adapter.delete(key: 'afwal.auth.session.v1');

    expect(await adapter.read(key: 'afwal.auth.session.v1'), isNull);
    expect(await adapter.read(key: 'unrelated-key'), 'keep-me');
  });
}
